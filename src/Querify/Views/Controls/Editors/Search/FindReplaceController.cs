using CodeEdit.Maui;
using RichEdit.Maui;
using System.ComponentModel;
using NativeCodeEditor = CodeEdit.Maui.CodeEditor;

namespace Querify.Views.Controls.Editors.Search;

/// <summary>
/// Rechercher / Remplacer d'un editeur : recherche a la frappe depuis le curseur, occurrences surlignees,
/// F3 / Maj+F3 en boucle, compteur, « Tout remplacer » en une seule etape d'annulation.
/// </summary>
public sealed class FindReplaceController : IDisposable
{
    /// <summary>Au-dela, le surlignage et le compteur s'arretent (texte tres repetitif).</summary>
    private const int MaxMatches = 10_000;

    private readonly NativeCodeEditor _editor;
    private readonly FindReplacePopup _popup;
    private readonly RichTextDecorationLayer _layer;
    private readonly RichTextDecorationStyle _matchStyle;
    private readonly List<RichTextRange> _matches = [];

    /// <summary>Modification faite par le controleur : sa propre relecture suit, pas celle differee.</summary>
    private bool _isEditing;

    private bool _isRefreshScheduled;
    private bool _isDisposed;

    public FindReplaceController(NativeCodeEditor editor, FindReplacePopup popup)
    {
        _editor = editor;
        _popup = popup;
        _layer = editor.Decorations.CreateLayer();
        _matchStyle = new RichTextDecorationStyle { BackgroundColor = ResourceColor("Warning", Colors.Gold).WithAlpha(0.3f) };

        _popup.FindNextCommand = new Command(() => Find(forward: true));
        _popup.FindPreviousCommand = new Command(() => Find(forward: false));
        _popup.ReplaceCommand = new Command(Replace);
        _popup.ReplaceAllCommand = new Command(ReplaceAll);
        _popup.CloseCommand = new Command(Close);

        _popup.PropertyChanged += OnPopupPropertyChanged;
        _editor.ContentChanged += OnEditorChanged;
        _editor.DocumentChanged += OnEditorChanged;

        if (IsOpen)
            Refresh(selectMatch: false);
    }

    public bool IsOpen => _popup.IsVisible;

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _popup.PropertyChanged -= OnPopupPropertyChanged;
        _editor.ContentChanged -= OnEditorChanged;
        _editor.DocumentChanged -= OnEditorChanged;
        _layer.Dispose();
    }

    /// <summary>Ctrl+F / Ctrl+H : une selection d'une ligne devient le texte cherche.</summary>
    public void Open(bool replace)
    {
        var selected = _editor.Selection.Text;
        if (!string.IsNullOrEmpty(selected) && !selected.Contains('\n'))
            _popup.SearchText = selected;

        _popup.IsReplaceMode = replace;
        _popup.IsVisible = true;
        Refresh(selectMatch: false);
        _popup.FocusSearch();
    }

    /// <summary>F3 / Maj+F3 depuis l'editeur : sans texte cherche, ouvre la barre.</summary>
    public void FindFromEditor(bool forward)
    {
        if (string.IsNullOrEmpty(_popup.SearchText))
        {
            Open(replace: false);
            return;
        }

        if (!IsOpen)
        {
            _popup.IsVisible = true;
            Refresh(selectMatch: false);
        }

        Find(forward);
    }

    public void Close()
    {
        _popup.IsVisible = false;
        _layer.Clear();
        _editor.Focus();
    }

    private void OnPopupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Recherche a la frappe : l'occurrence la plus proche du curseur est selectionnee.
        if (IsOpen && e.PropertyName is nameof(FindReplacePopup.SearchText) or nameof(FindReplacePopup.IsMatchCase))
            Refresh(selectMatch: true);
    }

    private void OnEditorChanged(object? sender, EventArgs e)
    {
        if (!IsOpen || _isEditing || _isRefreshScheduled || _isDisposed)
            return;

        _isRefreshScheduled = true;
        _editor.Dispatcher.Dispatch(() =>
        {
            _isRefreshScheduled = false;
            if (IsOpen && !_isDisposed)
                Refresh(selectMatch: false);
        });
    }

    private StringComparison Comparison => _popup.IsMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    /// <summary>Relit les occurrences et les surligne ; <paramref name="selectMatch"/> selectionne la premiere apres le curseur.</summary>
    private void Refresh(bool selectMatch)
    {
        var snapshot = _editor.Document.CurrentSnapshot;
        var text = snapshot.Text;
        var search = _popup.SearchText;

        _matches.Clear();
        if (!string.IsNullOrEmpty(search))
        {
            for (var index = text.IndexOf(search, Comparison); index >= 0 && _matches.Count < MaxMatches; index = text.IndexOf(search, index + search.Length, Comparison))
                _matches.Add(new RichTextRange(index, search.Length));
        }

        if (_matches.Count == 0)
            _layer.Clear();
        else
            _layer.TrySet(snapshot.Revision, _matches.Select(m => new RichTextDecoration(m, _matchStyle)));

        if (selectMatch && _matches.Count > 0)
        {
            var from = _editor.SelectionState.Range.Start;
            Select(_matches.FirstOrDefault(m => m.Start >= from, _matches[0]));
            return;
        }

        UpdateResult();
    }

    private void Find(bool forward)
    {
        if (_matches.Count == 0)
            Refresh(selectMatch: false);

        if (_matches.Count == 0)
            return;

        var start = _editor.SelectionState.Range.Start;
        var match = forward
            ? _matches.FirstOrDefault(m => m.Start > start, _matches[0])
            : _matches.LastOrDefault(m => m.Start < start, _matches[^1]);

        Select(match);
    }

    private void Select(RichTextRange match)
    {
        _editor.SelectionState = new RichTextSelectionState(match.Start, match.End);
        _editor.ScrollIntoView(match);
        UpdateResult();
    }

    private void UpdateResult()
    {
        if (string.IsNullOrEmpty(_popup.SearchText))
        {
            _popup.ResultText = string.Empty;
            return;
        }

        if (_matches.Count == 0)
        {
            _popup.ResultText = "Aucun résultat";
            return;
        }

        var total = _matches.Count >= MaxMatches ? $"{MaxMatches}+" : _matches.Count.ToString();
        var index = _matches.IndexOf(_editor.SelectionState.Range);
        _popup.ResultText = index >= 0 ? $"{index + 1} sur {total}" : $"{total} résultat(s)";
    }

    /// <summary>Remplace l'occurrence selectionnee, puis passe a la suivante.</summary>
    private void Replace()
    {
        var range = _editor.SelectionState.Range;
        if (!_matches.Contains(range))
        {
            Find(forward: true);
            return;
        }

        var snapshot = _editor.Document.CurrentSnapshot;
        var replacement = _popup.ReplaceText ?? string.Empty;
        var caret = range.Start + replacement.Length;

        _isEditing = true;
        try
        {
            if (!_editor.TryApplyEdits(snapshot.Revision, [new RichTextEdit(range, replacement)],
                new RichTextSelectionState(caret, caret), new RichTextEditOptions(RichTextUndoBehavior.CreateUnit, "Remplacer", null)))
                return;
        }
        finally
        {
            _isEditing = false;
        }

        Refresh(selectMatch: false);
        Find(forward: true);
    }

    /// <summary>Toutes les occurrences en une transaction : un seul Ctrl+Z annule le tout.</summary>
    private void ReplaceAll()
    {
        Refresh(selectMatch: false);
        if (_matches.Count == 0)
            return;

        var snapshot = _editor.Document.CurrentSnapshot;
        var replacement = _popup.ReplaceText ?? string.Empty;
        var count = _matches.Count;
        var edits = _matches.Select(m => new RichTextEdit(m, replacement)).ToList();

        // Curseur sur la fin du dernier remplacement.
        var last = _matches[^1];
        var caret = last.Start + (replacement.Length - last.Length) * (count - 1) + replacement.Length;

        _isEditing = true;
        try
        {
            if (!_editor.TryApplyEdits(snapshot.Revision, edits, new RichTextSelectionState(caret, caret),
                new RichTextEditOptions(RichTextUndoBehavior.CreateUnit, "Tout remplacer", null)))
                return;
        }
        finally
        {
            _isEditing = false;
        }

        Refresh(selectMatch: false);
        _popup.ResultText = $"{count} remplacé(s)";
    }

    private static Color ResourceColor(string key, Color fallback)
        => Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color ? color : fallback;
}
