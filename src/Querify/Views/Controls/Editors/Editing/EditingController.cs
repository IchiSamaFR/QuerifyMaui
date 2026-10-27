using CodeEdit.Maui;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Models.Editors.Highlighting;
using Querify.UI.Extensions;
using Quill.Code.Editing;
using RichEdit.Maui;
using NativeCodeEditor = CodeEdit.Maui.CodeEditor;

namespace Querify.Views.Controls.Editors.Editing;

/// <summary>
/// Raccourcis d'edition facon Visual Studio : indentation a l'Entree, paires de caracteres, retour arriere,
/// presse-papiers par ligne, operations sur les lignes, commentaires, Debut. Les touches passent par KeyDown ;
/// les caracteres tapes, qui dependent de la disposition du clavier, sont traites apres coup sur la
/// modification du document, fusionnes dans son unite d'annulation.
/// </summary>
public sealed class EditingController : IDisposable
{
    private static readonly RichTextEditOptions TypingOptions = new(RichTextUndoBehavior.MergeWithPrevious, "Saisie", null);

    private readonly NativeCodeEditor _editor;
    private readonly Func<SyntaxConfiguration?> _syntax;
    private readonly Func<bool> _isCompletionOpen;

    /// <summary>Texte avant la derniere modification : le texte remplace par une frappe n'est plus dans le document.</summary>
    private string _lastText;

    /// <summary>Ctrl+K recu : la touche suivante complete l'accord (Ctrl+C / Ctrl+U).</summary>
    private bool _isChordPending;

    /// <summary>Derniere ligne copiee ou coupee sans selection : collee, elle s'insere au-dessus de la ligne courante.</summary>
    private string? _lineClipboard;

    private bool _isClipboardBusy;

    /// <summary>L'editeur peut relayer deux fois la meme touche : la seconde est ignoree dans le meme passage.</summary>
    private (EditorKey Key, EditorKeyModifiers Modifiers)? _lastHandledKey;

    private bool _isDisposed;

    /// <param name="syntax">Langage courant : regles de chaines et prefixe de commentaire.</param>
    /// <param name="isCompletionOpen">Liste d'autocompletion ouverte : Entree, Tab et fleches lui reviennent.</param>
    public EditingController(NativeCodeEditor editor, Func<SyntaxConfiguration?> syntax, Func<bool> isCompletionOpen)
    {
        _editor = editor;
        _syntax = syntax;
        _isCompletionOpen = isCompletionOpen;
        _lastText = editor.Document.Text;

        _editor.KeyDown += OnKeyDown;
        _editor.ContentChanged += OnContentChanged;
        _editor.DocumentChanged += OnDocumentChanged;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _editor.KeyDown -= OnKeyDown;
        _editor.ContentChanged -= OnContentChanged;
        _editor.DocumentChanged -= OnDocumentChanged;
    }

    private SyntaxLanguage Language => _syntax()?.Language ?? SyntaxLanguage.None;

    private string CommentPrefix => _syntax()?.Comments is { Length: > 0 } prefix ? prefix : _editor.LineCommentPrefix;

    /// <summary>Reglages des commandes de Quill, relus a chaque commande (langage et options de l'editeur changent).</summary>
    private CodeEditingOptions Options => new()
    {
        IndentSize = _editor.IndentSize,
        UseTabs = _editor.UseTabs,
        Syntax = EditorSyntax.For(Language, CommentPrefix),
        InsertSpaceAfterLineComment = false,
    };

    // ---------- Touches ----------

    private void OnKeyDown(object? sender, EditorKeyEventArgs e)
    {
        // Une touche de modification seule ne rompt pas l'accord Ctrl+K.
        if (e.Handled || e.Key == EditorKey.None || _editor.IsReadOnly)
            return;

        if (_lastHandledKey == (e.Key, e.Modifiers))
        {
            e.Handled = true;
            return;
        }

        if (_isCompletionOpen() && e.Modifiers == EditorKeyModifiers.None
            && e.Key is EditorKey.Enter or EditorKey.Tab or EditorKey.Up or EditorKey.Down or EditorKey.PageUp or EditorKey.PageDown or EditorKey.Escape)
            return;

        e.Handled = Handle(e.Key, e.Modifiers);

        if (e.Handled)
        {
            _lastHandledKey = (e.Key, e.Modifiers);
            _editor.Dispatcher.Dispatch(() => _lastHandledKey = null);
        }
    }

    private bool Handle(EditorKey key, EditorKeyModifiers modifiers)
    {
        const EditorKeyModifiers None = EditorKeyModifiers.None;
        const EditorKeyModifiers Ctrl = EditorKeyModifiers.Control;
        const EditorKeyModifiers Shift = EditorKeyModifiers.Shift;
        const EditorKeyModifiers Alt = EditorKeyModifiers.Alt;

        if (_isChordPending)
        {
            _isChordPending = false;
            if (modifiers == Ctrl && key is EditorKey.C or EditorKey.U)
            {
                SetLineComments(comment: key == EditorKey.C);
                return true;
            }
        }

        var isEmpty = _editor.SelectionState.IsEmpty;

        switch (key, modifiers)
        {
            case (EditorKey.Enter, None or Shift):
                return InsertNewLine();
            case (EditorKey.Enter, Ctrl):
                return OpenLine(above: true);
            case (EditorKey.Enter, Ctrl | Shift):
                return OpenLine(above: false);
            case (EditorKey.Backspace, None):
                return DeleteBackward();
            case (EditorKey.Tab, None):
                return InsertTab();
            case (EditorKey.Home, None or Shift):
                return MoveHome(extend: modifiers == Shift);
            case (EditorKey.Up or EditorKey.Down, Alt):
                return MoveLines(up: key == EditorKey.Up);
            case (EditorKey.K, Ctrl):
                _isChordPending = true;
                return true;
            case (EditorKey.K, Ctrl | Shift):
                return DeleteLines();
            case (EditorKey.D, Ctrl):
                return Duplicate();
            case (EditorKey.C or EditorKey.X, Ctrl) when isEmpty:
                if (!_isClipboardBusy)
                    CopyLineAsync(cut: key == EditorKey.X).FireAndForget("Impossible de copier la ligne.");
                return true;
            case (EditorKey.C or EditorKey.X, Ctrl):
                _lineClipboard = null;
                return false;
            case (EditorKey.V, Ctrl) when isEmpty && _lineClipboard is not null:
                if (!_isClipboardBusy)
                    PasteAsync().FireAndForget("Impossible de coller.");
                return true;
            default:
                return false;
        }
    }

    // ---------- Commandes ----------

    /// <summary>Garde l'indentation ; apres { un niveau de plus ; entre { et } le bloc s'ouvre sur trois lignes.</summary>
    private bool InsertNewLine() => Run(CodeEditing.InsertNewLine, "Nouvelle ligne");

    /// <summary>Ctrl+Entree : ligne vide au-dessus ; Ctrl+Maj+Entree : en dessous (indentation de la ligne courante).</summary>
    private bool OpenLine(bool above) => Run((text, selection, options) => CodeEditing.OpenLine(text, selection, above, options), "Insérer une ligne");

    /// <summary>Paire vide supprimee d'un coup ; dans les espaces de tete, un niveau d'indentation entier.</summary>
    private bool DeleteBackward() => Run(CodeEditing.DeleteBackward, "Supprimer");

    /// <summary>Sans selection multiligne, des espaces jusqu'au taquet suivant ; sinon l'indentation native des lignes.</summary>
    private bool InsertTab()
    {
        var range = _editor.SelectionState.Range;
        if (_editor.Document.Text.AsSpan(range.Start, range.Length).Contains('\n'))
            return false;

        return Run(CodeEditing.InsertTab, "Tabulation");
    }

    /// <summary>Premier caractere non blanc, puis debut de ligne au second appui.</summary>
    private bool MoveHome(bool extend)
    {
        var selection = _editor.SelectionState;
        var target = CodeEditing.SmartHomePosition(_editor.Document.Text, selection.Active);

        _editor.SelectionState = new RichTextSelectionState(extend ? selection.Anchor : target, target);
        _editor.ScrollIntoView(new RichTextRange(target, 0));
        return true;
    }

    /// <summary>Alt+Haut / Alt+Bas : echange le bloc de lignes selectionne avec sa voisine.</summary>
    private bool MoveLines(bool up) => Run((text, selection, _) => CodeEditing.MoveLines(text, selection, up), "Déplacer les lignes") || true;

    /// <summary>Ctrl+Maj+K : supprime les lignes de la selection.</summary>
    private bool DeleteLines() => Run((text, selection, _) => CodeEditing.DeleteLines(text, selection), "Supprimer les lignes") || true;

    /// <summary>Ctrl+D : duplique la selection (la copie est selectionnee), ou la ligne.</summary>
    private bool Duplicate() => Run(CodeEditing.DuplicateSelectionOrLine, "Dupliquer") || true;

    /// <summary>Ctrl+K, Ctrl+C / Ctrl+K, Ctrl+U : prefixe pose au niveau d'indentation minimal des lignes non vides.</summary>
    private void SetLineComments(bool comment)
        => Run((text, selection, options) => CodeEditing.ToggleLineComment(text, selection, comment, options), comment ? "Commenter" : "Décommenter");

    // ---------- Presse-papiers ----------

    /// <summary>Ctrl+C / Ctrl+X sans selection : la ligne entiere, avec son retour.</summary>
    private async Task CopyLineAsync(bool cut)
    {
        _isClipboardBusy = true;
        try
        {
            var snapshot = _editor.Document.CurrentSnapshot;
            var text = snapshot.Text;
            var caret = _editor.SelectionState.Active;
            var lineStart = TextLines.LineStart(text, caret);
            var lineEnd = TextLines.LineEnd(text, caret);
            var line = text[lineStart..lineEnd];

            // Presse-papiers d'abord : s'il echoue, la ligne n'est pas perdue.
            await Clipboard.Default.SetTextAsync(line + Environment.NewLine);
            _lineClipboard = line + "\n";

            if (!cut || text.Length == 0)
                return;

            int removeStart, removeEnd;
            if (lineEnd < text.Length)
            {
                removeStart = lineStart;
                removeEnd = lineEnd + 1;
            }
            else
            {
                removeStart = lineStart > 0 ? lineStart - 1 : 0;
                removeEnd = lineEnd;
            }

            // Texte modifie pendant l'ecriture du presse-papiers : la plage ne designe plus la ligne.
            if (_editor.Document.Revision != snapshot.Revision)
                return;

            Apply(snapshot, [new RichTextEdit(FromBounds(removeStart, removeEnd), string.Empty)], removeStart, "Couper la ligne");
        }
        finally
        {
            _isClipboardBusy = false;
        }
    }

    /// <summary>Ctrl+V d'une ligne copiee sans selection : collee au-dessus de la ligne courante, comme dans VS.</summary>
    private async Task PasteAsync()
    {
        _isClipboardBusy = true;
        try
        {
            var clipboard = (await Clipboard.Default.GetTextAsync())?.Replace("\r\n", "\n").Replace('\r', '\n');

            var snapshot = _editor.Document.CurrentSnapshot;
            var selection = _editor.SelectionState;
            if (clipboard is null || clipboard != _lineClipboard || !selection.IsEmpty)
            {
                await _editor.PasteAsync();
                return;
            }

            var lineStart = TextLines.LineStart(snapshot.Text, selection.Active);
            Apply(snapshot, [new RichTextEdit(new RichTextRange(lineStart, 0), clipboard)], selection.Active + clipboard.Length, "Coller la ligne");
        }
        finally
        {
            _isClipboardBusy = false;
        }
    }

    // ---------- Caracteres tapes ----------

    private void OnDocumentChanged(object? sender, CodeDocumentReplacedEventArgs e) => _lastText = _editor.Document.Text;

    private void OnContentChanged(object? sender, RichTextContentChangedEventArgs e)
    {
        var previous = _lastText;
        _lastText = _editor.Document.Text;

        var changeSet = e.ChangeSet;
        if (changeSet.Origin != RichTextChangeOrigin.User || changeSet.Changes is not [{ Kind: RichTextChangeKind.Text } change])
            return;

        if (change.NewRange.Length != 1 || change.OldRange.End > previous.Length)
            return;

        var typed = _lastText[change.NewRange.Start];
        if (CodeEditing.ClosingOf(typed) is null && typed is not (')' or ']' or '}'))
            return;

        var start = change.OldRange.Start;
        var replaced = previous.Substring(start, change.OldRange.Length);
        var revision = _editor.Document.Revision;

        // On ne modifie pas le document pendant qu'il notifie sa propre modification.
        _editor.Dispatcher.Dispatch(() => OnTyped(revision, typed, start, replaced, previous));
    }

    /// <summary>
    /// La frappe est rejouee par Quill sur le texte d'avant (paires, fermant saute, selection entouree, } desindentee) :
    /// seule la difference avec le document courant est appliquee, fusionnee dans l'unite d'annulation de la frappe.
    /// </summary>
    private void OnTyped(RichTextRevision revision, char typed, int start, string replaced, string previous)
    {
        if (_isDisposed || _editor.IsReadOnly || _editor.Document.Revision != revision)
            return;

        var result = CodeEditing.TypeCharacter(previous, new TextSelection(start, start + replaced.Length), typed, Options);
        if (result is null)
            return;

        var snapshot = _editor.Document.CurrentSnapshot;
        var selection = new RichTextSelectionState(result.Selection.Anchor, result.Selection.Active);

        if (TextEdit.Between(snapshot.Text, result.Apply(previous)) is not { } edit)
        {
            _editor.SelectionState = selection;
            return;
        }

        _editor.TryApplyEdits(snapshot.Revision, [ToRichTextEdit(edit)], selection, TypingOptions);
    }

    // ---------- Outils ----------

    /// <summary>Commande de Quill sur le texte et la selection courants ; null : la touche garde son effet normal.</summary>
    private bool Run(Func<string, TextSelection, CodeEditingOptions, EditResult?> command, string description)
    {
        var snapshot = _editor.Document.CurrentSnapshot;
        var current = _editor.SelectionState;
        var result = command(snapshot.Text, new TextSelection(current.Anchor, current.Active), Options);
        if (result is null)
            return false;

        var selection = new RichTextSelectionState(result.Selection.Anchor, result.Selection.Active);
        if (result.Edits.Count == 0)
        {
            _editor.SelectionState = selection;
            return true;
        }

        return Apply(snapshot, [.. result.Edits.Select(ToRichTextEdit)], selection, description);
    }

    private bool Apply(CodeDocumentSnapshot snapshot, IReadOnlyList<RichTextEdit> edits, int caret, string description)
        => Apply(snapshot, edits, new RichTextSelectionState(caret, caret), description);

    private bool Apply(CodeDocumentSnapshot snapshot, IReadOnlyList<RichTextEdit> edits, RichTextSelectionState selection, string description)
    {
        var applied = _editor.TryApplyEdits(snapshot.Revision, edits, selection, new RichTextEditOptions(RichTextUndoBehavior.CreateUnit, description, null));
        if (applied)
            _editor.ScrollIntoView(new RichTextRange(selection.Active, 0));
        return applied;
    }

    private static RichTextEdit ToRichTextEdit(TextEdit edit) => new(new RichTextRange(edit.Start, edit.Length), edit.NewText);

    private static RichTextRange FromBounds(int start, int end) => new(start, end - start);
}
