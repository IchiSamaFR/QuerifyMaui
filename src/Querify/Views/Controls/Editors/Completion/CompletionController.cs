using CodeEdit.Maui;
using Quill.Code;
using Quill.Code.Completion;
using RichEdit.Maui;
using Querify.UI.Extensions;
using NativeCodeEditor = CodeEdit.Maui.CodeEditor;

namespace Querify.Views.Controls.Editors.Completion;

/// <summary>
/// Pilote l'autocompletion d'un editeur natif : ouverture a la frappe ou sur Ctrl+Espace,
/// navigation au clavier dans <see cref="CompletionPopup"/>, insertion de la suggestion.
/// Les candidats sont charges une fois par ouverture, puis seulement refiltres a chaque touche.
/// </summary>
public sealed class CompletionController : IDisposable
{
    private const double CaretGap = 2;

    /// <summary>Laisse passer une rafale de frappe avant de charger les candidats.</summary>
    private static readonly TimeSpan TypingDelay = TimeSpan.FromMilliseconds(120);

    private static readonly RichTextEditOptions CommitOptions = new(RichTextUndoBehavior.CreateUnit, "Autocompletion", null);

    private readonly NativeCodeEditor _editor;
    private readonly CompletionPopup _popup;
    private readonly View _host;
    private readonly Func<ICodeLanguageService?> _service;
    private readonly RichTextLayout _layout;
    private readonly RichEditor? _focusTarget;

    private CompletionSession? _session;
    private CompletionTrigger? _pendingTrigger;
    private CancellationTokenSource? _pending;
    private bool _isRefreshScheduled;
    private bool _isCommitting;
    private bool _isDisposed;

    /// <param name="host">Conteneur commun a l'editeur et au popup : repere des coordonnees du popup.</param>
    /// <param name="service">Service Quill du contexte de l'editeur, lu a chaque ouverture (connexion, base, parametres) ; null : pas de liste.</param>
    public CompletionController(NativeCodeEditor editor, CompletionPopup popup, View host, Func<ICodeLanguageService?> service)
    {
        _editor = editor;
        _popup = popup;
        _host = host;
        _service = service;
        _layout = editor.TextLayout.CreateRelativeTo(host);

        // Le focus est porte par le RichEditor interne, pas par le conteneur CodeEdit.
        _focusTarget = editor.GetVisualTreeDescendants().OfType<RichEditor>().FirstOrDefault();

        _editor.KeyDown += OnKeyDown;
        _editor.TextChanged += OnTextChanged;
        _editor.SelectionChanged += OnSelectionChanged;
        _editor.DocumentChanged += OnDocumentChanged;
        _layout.Changed += OnLayoutChanged;
        _popup.ItemInvoked += OnItemInvoked;

        if (_focusTarget is not null)
            _focusTarget.Unfocused += OnUnfocused;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Close();

        _editor.KeyDown -= OnKeyDown;
        _editor.TextChanged -= OnTextChanged;
        _editor.SelectionChanged -= OnSelectionChanged;
        _editor.DocumentChanged -= OnDocumentChanged;
        _layout.Changed -= OnLayoutChanged;
        _popup.ItemInvoked -= OnItemInvoked;

        if (_focusTarget is not null)
            _focusTarget.Unfocused -= OnUnfocused;
    }

    // ---------- Evenements de l'editeur ----------

    private void OnKeyDown(object? sender, EditorKeyEventArgs e)
    {
        if (e.Key == EditorKey.Space && e.Modifiers == EditorKeyModifiers.Control)
        {
            e.Handled = true;
            OpenExplicitly();
            return;
        }

        if (_session is null || !_popup.IsVisible || e.Modifiers != EditorKeyModifiers.None)
            return;

        switch (e.Key)
        {
            case EditorKey.Down:
                _popup.MoveSelection(1);
                break;
            case EditorKey.Up:
                _popup.MoveSelection(-1);
                break;
            case EditorKey.PageDown:
                _popup.MoveSelection(CompletionPopup.VisibleRows);
                break;
            case EditorKey.PageUp:
                _popup.MoveSelection(-CompletionPopup.VisibleRows);
                break;
            case EditorKey.Enter or EditorKey.Tab when _popup.SelectedItem is { } item:
                Commit(item);
                break;
            case EditorKey.Escape:
                Close();
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void OnTextChanged(object? sender, RichTextTextChangedEventArgs e)
    {
        if (_isCommitting)
            return;

        // Saisie IME en cours, undo/redo, collage ou edition programmatique : on ne propose rien.
        if (_editor.Composition.IsActive || e.ChangeSet.Origin != RichTextChangeOrigin.User || e.Changes.Count != 1)
        {
            Close();
            return;
        }

        var change = e.Changes[0];
        var inserted = change.InsertedText ?? string.Empty;

        if (inserted.Length == 0 && change.RemovedLength > 0)
        {
            // Retour arriere : la liste suit le prefixe raccourci, mais ne s'ouvre pas.
            if (_session is not null)
                ScheduleRefresh();
            return;
        }

        if (inserted.Length != 1)
        {
            Close();
            return;
        }

        if (IsIdentifierChar(inserted[0]))
        {
            if (_session is null)
                _pendingTrigger ??= CompletionTrigger.Typing;

            ScheduleRefresh();
        }
        else if (inserted[0] == '.')
        {
            Close();
            _pendingTrigger = CompletionTrigger.MemberAccess;
            ScheduleRefresh();
        }
        else
        {
            Close();
        }
    }

    private void OnSelectionChanged(object? sender, RichTextSelectionChangedEventArgs e)
    {
        if (_session is not null)
            ScheduleRefresh();
    }

    private void OnDocumentChanged(object? sender, CodeDocumentReplacedEventArgs e) => Close();

    private void OnLayoutChanged(object? sender, EventArgs e)
    {
        if (_session is not null && _popup.IsVisible)
            UpdatePosition();
    }

    /// <summary>Differe : un clic dans la liste peut faire perdre le focus juste avant le Tapped.</summary>
    private void OnUnfocused(object? sender, FocusEventArgs e)
    {
        _editor.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () =>
        {
            if (_focusTarget is { IsFocused: false })
                Close();
        });
    }

    private void OnItemInvoked(object? sender, CompletionItem item)
    {
        Commit(item);
        _editor.Focus();
    }

    // ---------- Session ----------

    private void OpenExplicitly()
    {
        if (_session is not null || !_editor.SelectionState.IsEmpty)
            return;

        var text = _editor.Document.Text;
        var caret = _editor.SelectionState.Active;
        StartSessionAsync(CompletionTrigger.Explicit, text, caret, FindWordStart(text, caret), false).FireAndForget("Autocomplétion indisponible.");
    }

    /// <summary>TextChanged et SelectionChanged arrivent ensemble a chaque frappe : un seul recalcul.</summary>
    private void ScheduleRefresh()
    {
        if (_isRefreshScheduled)
            return;

        _isRefreshScheduled = true;
        _editor.Dispatcher.Dispatch(Refresh);
    }

    private void Refresh()
    {
        _isRefreshScheduled = false;
        if (_isDisposed)
            return;

        var selection = _editor.SelectionState;
        if (!selection.IsEmpty)
        {
            Close();
            return;
        }

        var text = _editor.Document.Text;
        var caret = selection.Active;
        var wordStart = FindWordStart(text, caret);

        if (_session is null)
        {
            if (_pendingTrigger is not { } trigger)
                return;

            _pendingTrigger = null;

            // Pas de liste sur un nombre (TOP 10, = 3).
            if (trigger == CompletionTrigger.Typing && (caret == wordStart || char.IsDigit(text[wordStart])))
                return;

            StartSessionAsync(trigger, text, caret, wordStart).FireAndForget("Autocomplétion indisponible.");
            return;
        }

        // Le curseur a quitte le mot de depart, ou l'a entierement efface.
        if (wordStart != _session.WordStart || (caret == wordStart && _session.Trigger == CompletionTrigger.Typing))
        {
            Close();
            return;
        }

        Show(CompletionFilter.Filter(_session.Candidates, text[wordStart..caret]));
    }

    private async Task StartSessionAsync(CompletionTrigger trigger, string text, int caret, int wordStart, bool isDelay = true)
    {
        CancelPending();
        if (_service() is not { } service)
            return;

        var cancellation = new CancellationTokenSource();
        _pending = cancellation;

        IReadOnlyList<CompletionItem> candidates;
        try
        {
            // Dans le try : une frappe qui remplace la demande ne doit pas laisser d'exception non observee.
            if (isDelay)
                await Task.Delay(TypingDelay, cancellation.Token);

            // Hors du thread UI : Roslyn ou l'analyse T-SQL ont une partie synchrone qui figerait la frappe.
            candidates = (await Task.Run(() => service.GetCompletionsAsync(text, caret, cancellation.Token), cancellation.Token)).Items;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellation.IsCancellationRequested || _isDisposed)
            return;

        _session = new CompletionSession(wordStart, trigger, candidates);

        // Le texte a pu bouger pendant le chargement : on revalide sur l'etat courant.
        Refresh();
    }

    private void Show(IReadOnlyList<CompletionItem> items)
    {
        if (items.Count == 0)
        {
            Close();
            return;
        }

        _popup.SetItems(items);
        UpdatePosition();
    }

    private void Close()
    {
        CancelPending();
        _pendingTrigger = null;
        _session = null;
        _popup.Hide();
    }

    private void CancelPending()
    {
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = null;
    }

    // ---------- Insertion ----------

    private void Commit(CompletionItem item)
    {
        if (_session is not { } session)
            return;

        var caret = _editor.SelectionState.Active;
        Close();

        if (caret < session.WordStart)
            return;

        var text = item.TextToInsert;
        var end = session.WordStart + text.Length;

        _isCommitting = true;
        try
        {
            _editor.TryApplyEdits(
                _editor.Document.CurrentSnapshot.Revision,
                [new RichTextEdit(new RichTextRange(session.WordStart, caret - session.WordStart), text)],
                new RichTextSelectionState(end, end),
                CommitOptions);
        }
        finally
        {
            _isCommitting = false;
        }
    }

    // ---------- Placement ----------

    /// <summary>Sous le curseur, ou au-dessus s'il n'y a pas la place en bas de l'editeur.</summary>
    private void UpdatePosition()
    {
        var snapshot = _layout.Capture();
        if (_layout.GetCaretBounds(snapshot, _editor.SelectionState.Active) is not { } caret
            || caret.Bottom < 0 || caret.Top > _host.Height)
        {
            Close();
            return;
        }

        var height = _popup.PopupHeight;
        var y = caret.Bottom + CaretGap;
        if (y + height > _host.Height && caret.Top - height - CaretGap >= 0)
            y = caret.Top - height - CaretGap;

        // Jamais plus large que l'editeur.
        _popup.MaximumWidthRequest = Math.Min(CompletionPopup.MaxPopupWidth, _host.Width);

        _popup.TranslationX = Math.Clamp(caret.Left, 0, Math.Max(0, _host.Width - _popup.PopupWidth));
        _popup.TranslationY = y;
        _popup.IsVisible = true;
    }

    /// <summary>@ et # pour les variables et tables temporaires T-SQL.</summary>
    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';

    private static int FindWordStart(string text, int caret)
    {
        var start = Math.Clamp(caret, 0, text.Length);
        while (start > 0 && IsIdentifierChar(text[start - 1]))
            start--;

        return start;
    }

    private sealed record CompletionSession(int WordStart, CompletionTrigger Trigger, IReadOnlyList<CompletionItem> Candidates);
}
