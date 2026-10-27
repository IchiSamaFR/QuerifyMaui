using CodeEdit.Maui;
using Quill.Code.Analysis;
using RichEdit.Maui;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Models.Editors.Analysis;
using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;
using Querify.Services.Editors.Analysis;
using Querify.UI.Extensions;
using System.Globalization;
using NativeCodeEditor = CodeEdit.Maui.CodeEditor;

namespace Querify.Views.Controls.Editors.Analysis;

/// <summary>
/// Aide a la signature : a la frappe de "(" ou "," (ou sur Ctrl+Maj+Espace), affiche au-dessus du curseur
/// la methode appelee, parametre courant en gras. Haut / Bas parcourent les surcharges, Echap ferme ;
/// elle se ferme d'elle-meme quand le curseur sort des parentheses.
/// </summary>
public sealed class SignatureHelpController : IDisposable
{
    private const double Gap = 4;

    /// <summary>Laisse passer une rafale de frappe avant d'interroger Roslyn.</summary>
    private static readonly TimeSpan TypingDelay = TimeSpan.FromMilliseconds(80);

    private readonly NativeCodeEditor _editor;
    private readonly SignatureHelpPopup _popup;
    private readonly View _host;
    private readonly ICodeAnalysisService _service;
    private readonly SyntaxLanguage _language;
    private readonly Func<DatabaseConnectionInfo?> _connection;
    private readonly Func<string?> _database;
    private readonly Func<IReadOnlyList<QueryParameter>?> _parameters;
    private readonly Func<bool> _isCompletionOpen;
    private readonly RichTextLayout _layout;
    private readonly RichEditor? _focusTarget;

    private SignatureHelpInfo? _current;
    private int _selected;
    private CancellationTokenSource? _pending;
    private bool _isRefreshScheduled;
    private bool _isDisposed;

    /// <param name="host">Conteneur commun a l'editeur et au popup : repere des coordonnees du popup.</param>
    /// <param name="isCompletionOpen">Vrai quand la liste d'autocompletion est ouverte : Haut / Bas lui reviennent.</param>
    public SignatureHelpController(NativeCodeEditor editor, SignatureHelpPopup popup, View host, ICodeAnalysisService service, SyntaxLanguage language,
        Func<DatabaseConnectionInfo?> connection, Func<string?> database, Func<IReadOnlyList<QueryParameter>?> parameters, Func<bool> isCompletionOpen)
    {
        ArgumentNullException.ThrowIfNull(editor);

        _editor = editor;
        _popup = popup;
        _host = host;
        _service = service;
        _language = language;
        _connection = connection;
        _database = database;
        _parameters = parameters;
        _isCompletionOpen = isCompletionOpen;
        _layout = editor.TextLayout.CreateRelativeTo(host);

        // Le focus est porte par le RichEditor interne, pas par le conteneur CodeEdit.
        _focusTarget = editor.GetVisualTreeDescendants().OfType<RichEditor>().FirstOrDefault();

        _editor.KeyDown += OnKeyDown;
        _editor.TextChanged += OnTextChanged;
        _editor.SelectionChanged += OnSelectionChanged;
        _editor.DocumentChanged += OnDocumentChanged;
        _layout.Changed += OnLayoutChanged;
        _popup.SizeChanged += OnPopupSizeChanged;

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
        _popup.SizeChanged -= OnPopupSizeChanged;

        if (_focusTarget is not null)
            _focusTarget.Unfocused -= OnUnfocused;
    }

    // ---------- Evenements de l'editeur ----------

    private void OnKeyDown(object? sender, EditorKeyEventArgs e)
    {
        if (e.Key == EditorKey.Space && e.Modifiers == (EditorKeyModifiers.Control | EditorKeyModifiers.Shift))
        {
            e.Handled = true;
            ScheduleRefresh();
            return;
        }

        // L'autocompletion, abonnee avant, a pu traiter la touche (Echap ferme d'abord sa liste).
        if (_current is null || e.Handled || e.Modifiers != EditorKeyModifiers.None)
            return;

        switch (e.Key)
        {
            case EditorKey.Escape:
                Close();
                break;
            case EditorKey.Up when _current.Signatures.Count > 1 && !_isCompletionOpen():
                Select(_selected - 1);
                break;
            case EditorKey.Down when _current.Signatures.Count > 1 && !_isCompletionOpen():
                Select(_selected + 1);
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void OnTextChanged(object? sender, RichTextTextChangedEventArgs e)
    {
        if (_current is not null)
        {
            ScheduleRefresh();
            return;
        }

        // Ouverture a l'ouverture d'une parenthese ou au passage a l'argument suivant.
        if (e.ChangeSet.Origin == RichTextChangeOrigin.User && e.Changes.Count == 1 && e.Changes[0].InsertedText is "(" or ",")
            ScheduleRefresh();
    }

    /// <summary>Le curseur deplace (fleches, clic) peut changer d'argument ou sortir de l'appel.</summary>
    private void OnSelectionChanged(object? sender, RichTextSelectionChangedEventArgs e)
    {
        if (_current is not null)
            ScheduleRefresh();
    }

    private void OnDocumentChanged(object? sender, CodeDocumentReplacedEventArgs e) => Close();

    private void OnLayoutChanged(object? sender, EventArgs e)
    {
        if (_current is not null && _popup.IsVisible)
            UpdatePosition();
    }

    private void OnPopupSizeChanged(object? sender, EventArgs e)
    {
        if (_current is not null && _popup.IsVisible)
            UpdatePosition();
    }

    private void OnUnfocused(object? sender, FocusEventArgs e)
    {
        _editor.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () =>
        {
            if (_focusTarget is { IsFocused: false })
                Close();
        });
    }

    // ---------- Session ----------

    /// <summary>TextChanged et SelectionChanged arrivent ensemble a chaque frappe : un seul calcul.</summary>
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

        if (!_editor.SelectionState.IsEmpty)
        {
            Close();
            return;
        }

        CancelPending();
        var cancellation = new CancellationTokenSource();
        _pending = cancellation;

        RefreshAsync(_editor.Document.Text, _editor.SelectionState.Active, cancellation.Token).FireAndForget("Aide à la signature indisponible.");
    }

    private async Task RefreshAsync(string text, int caret, CancellationToken cancellationToken)
    {
        SignatureHelpInfo? help;
        try
        {
            await Task.Delay(TypingDelay, cancellationToken);

            help = await _service.GetSignatureHelpAsync(new CodeAnalysisRequest
            {
                Language = _language,
                Text = text,
                Offset = caret,
                Connection = _connection(),
                Database = _database(),
                Parameters = _parameters() ?? [],
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested || _isDisposed)
            return;

        if (help is null || help.Signatures.Count == 0)
        {
            Close();
            return;
        }

        // Meme appel : la surcharge choisie aux fleches est conservee ; sinon celle du compilateur.
        _selected = _current is { } previous && previous.ArgumentListStart == help.ArgumentListStart && previous.Signatures.Count == help.Signatures.Count
            ? Math.Clamp(_selected, 0, help.Signatures.Count - 1)
            : Math.Clamp(help.ActiveSignature, 0, help.Signatures.Count - 1);

        _current = help;
        Render();
    }

    private void Select(int index)
    {
        if (_current is not { } current)
            return;

        // Les fleches tournent en boucle sur les surcharges.
        _selected = (index + current.Signatures.Count) % current.Signatures.Count;
        Render();
    }

    private void Render()
    {
        if (_current is not { } current)
            return;

        var signature = current.Signatures[_selected];
        var parameters = signature.Parameters;
        var active = Math.Min(current.ActiveParameter, parameters.Count - 1);

        // Argument au-dela du dernier parametre (params) : le dernier reste en gras.
        if (active < 0)
        {
            _popup.Before = signature.Prefix;
            _popup.Active = null;
            _popup.After = signature.Suffix;
        }
        else
        {
            _popup.Before = signature.Prefix + string.Concat(parameters.Take(active).Select(p => p + ", "));
            _popup.Active = parameters[active];
            _popup.After = string.Concat(parameters.Skip(active + 1).Select(p => ", " + p)) + signature.Suffix;
        }

        _popup.Counter = current.Signatures.Count > 1
            ? string.Create(CultureInfo.InvariantCulture, $"{_selected + 1}/{current.Signatures.Count}")
            : null;
        _popup.Description = signature.Description;

        _popup.IsVisible = true;
        UpdatePosition();
    }

    private void Close()
    {
        CancelPending();
        _current = null;
        _popup.IsVisible = false;
    }

    private void CancelPending()
    {
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = null;
    }

    // ---------- Placement ----------

    /// <summary>Au-dessus de la ligne du curseur (l'autocompletion s'ouvre en dessous), sinon en dessous.</summary>
    private void UpdatePosition()
    {
        if (_current is not { } current)
            return;

        var snapshot = _layout.Capture();
        if (_layout.GetCaretBounds(snapshot, _editor.SelectionState.Active) is not { } caret
            || caret.Bottom < 0 || caret.Top > _host.Height)
        {
            Close();
            return;
        }

        // Aligne sur la parenthese ouvrante quand elle est sur la meme ligne.
        var left = _layout.GetCaretBounds(snapshot, current.ArgumentListStart) is { } open && Math.Abs(open.Top - caret.Top) < 1
            ? open.Left
            : caret.Left;

        var height = _popup.Height > 0 ? _popup.Height : 0;
        var width = _popup.Width > 0 ? _popup.Width : SignatureHelpPopup.MaxPopupWidth;

        var y = caret.Top - height - Gap;
        if (y < 0)
            y = caret.Bottom + Gap;

        _popup.TranslationX = Math.Clamp(left, 0, Math.Max(0, _host.Width - width));
        _popup.TranslationY = y;
    }
}
