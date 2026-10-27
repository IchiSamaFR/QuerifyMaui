using CodeEdit.Maui;
using Querify.Core.Enums.Editors;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Models.Editors.Highlighting;
using Querify.UI.Extensions;
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

    private static readonly Dictionary<char, char> Pairs = new()
    {
        ['('] = ')',
        ['['] = ']',
        ['{'] = '}',
        ['"'] = '"',
        ['\''] = '\'',
    };

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

    private string IndentUnit => _editor.UseTabs ? "\t" : new string(' ', _editor.IndentSize);

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

    // ---------- Entree ----------

    /// <summary>Garde l'indentation ; apres { un niveau de plus ; entre { et } le bloc s'ouvre sur trois lignes.</summary>
    private bool InsertNewLine()
    {
        var snapshot = _editor.Document.CurrentSnapshot;
        var text = snapshot.Text;
        var range = _editor.SelectionState.Range;

        var lineStart = LineStart(text, range.Start);
        var indent = text[lineStart..(lineStart + LeadingWhitespaceLength(text, lineStart, range.Start))];

        var before = range.Start;
        while (before > lineStart && text[before - 1] is ' ' or '\t')
            before--;

        var after = range.End;
        while (after < text.Length && text[after] is ' ' or '\t')
            after++;

        string inserted;
        int caret;
        var end = range.End;

        if (before > lineStart && text[before - 1] == '{')
        {
            var inner = indent + IndentUnit;
            inserted = "\n" + inner;
            caret = range.Start + inserted.Length;

            if (after < text.Length && text[after] == '}')
            {
                inserted += "\n" + indent;
                end = after;
            }
        }
        else
        {
            inserted = "\n" + indent;
            caret = range.Start + inserted.Length;
        }

        return Apply(snapshot, [new RichTextEdit(FromBounds(range.Start, end), inserted)], caret, "Nouvelle ligne");
    }

    /// <summary>Ctrl+Entree : ligne vide au-dessus ; Ctrl+Maj+Entree : en dessous (indentation de la ligne courante).</summary>
    private bool OpenLine(bool above)
    {
        var snapshot = _editor.Document.CurrentSnapshot;
        var text = snapshot.Text;
        var active = _editor.SelectionState.Active;

        var lineStart = LineStart(text, active);
        var lineEnd = LineEnd(text, active);
        var indent = text[lineStart..(lineStart + LeadingWhitespaceLength(text, lineStart, lineEnd))];

        return above
            ? Apply(snapshot, [new RichTextEdit(new RichTextRange(lineStart, 0), indent + "\n")], lineStart + indent.Length, "Insérer une ligne")
            : Apply(snapshot, [new RichTextEdit(new RichTextRange(lineEnd, 0), "\n" + indent)], lineEnd + 1 + indent.Length, "Insérer une ligne");
    }

    // ---------- Retour arriere et tabulation ----------

    /// <summary>Paire vide supprimee d'un coup ; dans les espaces de tete, un niveau d'indentation entier.</summary>
    private bool DeleteBackward()
    {
        var selection = _editor.SelectionState;
        if (!selection.IsEmpty || selection.Active == 0)
            return false;

        var snapshot = _editor.Document.CurrentSnapshot;
        var text = snapshot.Text;
        var position = selection.Active;

        if (position < text.Length && Pairs.TryGetValue(text[position - 1], out var close) && text[position] == close)
            return Apply(snapshot, [new RichTextEdit(new RichTextRange(position - 1, 2), string.Empty)], position - 1, "Supprimer");

        if (_editor.UseTabs || text[position - 1] != ' ')
            return false;

        var lineStart = LineStart(text, position);
        var column = position - lineStart;
        if (LeadingWhitespaceLength(text, lineStart, position) != column || text.AsSpan(lineStart, column).Contains('\t'))
            return false;

        var size = _editor.IndentSize;
        var count = Math.Min(column % size == 0 ? size : column % size, column);
        return Apply(snapshot, [new RichTextEdit(new RichTextRange(position - count, count), string.Empty)], position - count, "Supprimer");
    }

    /// <summary>Sans selection multiligne, des espaces jusqu'au taquet suivant ; sinon l'indentation native des lignes.</summary>
    private bool InsertTab()
    {
        var snapshot = _editor.Document.CurrentSnapshot;
        var text = snapshot.Text;
        var range = _editor.SelectionState.Range;

        if (text.AsSpan(range.Start, range.Length).Contains('\n'))
            return false;

        var inserted = "\t";
        if (!_editor.UseTabs)
        {
            var size = _editor.IndentSize;
            var column = VisualColumn(text, LineStart(text, range.Start), range.Start, size);
            inserted = new string(' ', size - column % size);
        }

        return Apply(snapshot, [new RichTextEdit(range, inserted)], range.Start + inserted.Length, "Tabulation");
    }

    // ---------- Debut ----------

    /// <summary>Premier caractere non blanc, puis debut de ligne au second appui.</summary>
    private bool MoveHome(bool extend)
    {
        var text = _editor.Document.Text;
        var selection = _editor.SelectionState;
        var active = selection.Active;

        var lineStart = LineStart(text, active);
        var firstChar = lineStart + LeadingWhitespaceLength(text, lineStart, LineEnd(text, active));
        var target = active == firstChar ? lineStart : firstChar;

        _editor.SelectionState = new RichTextSelectionState(extend ? selection.Anchor : target, target);
        _editor.ScrollIntoView(new RichTextRange(target, 0));
        return true;
    }

    // ---------- Lignes ----------

    /// <summary>Alt+Haut / Alt+Bas : echange le bloc de lignes selectionne avec sa voisine.</summary>
    private bool MoveLines(bool up)
    {
        var snapshot = _editor.Document.CurrentSnapshot;
        var text = snapshot.Text;
        var selection = _editor.SelectionState;
        var (start, end) = SelectedLines(text, selection.Range);
        var block = text[start..end];

        if (up)
        {
            if (start == 0)
                return true;

            var previousStart = LineStart(text, start - 1);
            var previous = text[previousStart..(start - 1)];
            var delta = -(previous.Length + 1);

            return Apply(snapshot, [new RichTextEdit(FromBounds(previousStart, end), block + "\n" + previous)],
                new RichTextSelectionState(selection.Anchor + delta, selection.Active + delta), "Déplacer les lignes") || true;
        }

        if (end >= text.Length)
            return true;

        var nextEnd = LineEnd(text, end + 1);
        var nextLine = text[(end + 1)..nextEnd];
        var shift = nextLine.Length + 1;

        return Apply(snapshot, [new RichTextEdit(FromBounds(start, nextEnd), nextLine + "\n" + block)],
            new RichTextSelectionState(selection.Anchor + shift, selection.Active + shift), "Déplacer les lignes") || true;
    }

    /// <summary>Ctrl+Maj+K : supprime les lignes de la selection.</summary>
    private bool DeleteLines()
    {
        var snapshot = _editor.Document.CurrentSnapshot;
        var text = snapshot.Text;
        var (start, end) = SelectedLines(text, _editor.SelectionState.Range);

        // Derniere ligne : c'est le retour qui la precede qui part, pour ne pas laisser de ligne vide.
        if (end < text.Length)
            return Apply(snapshot, [new RichTextEdit(FromBounds(start, end + 1), string.Empty)], start, "Supprimer les lignes") || true;

        var removeStart = start > 0 ? start - 1 : 0;
        var caret = start > 0 ? LineStart(text, start - 1) : 0;
        return Apply(snapshot, [new RichTextEdit(FromBounds(removeStart, end), string.Empty)], caret, "Supprimer les lignes") || true;
    }

    /// <summary>Ctrl+D : duplique la selection (la copie est selectionnee), ou la ligne.</summary>
    private bool Duplicate()
    {
        var snapshot = _editor.Document.CurrentSnapshot;
        var text = snapshot.Text;
        var selection = _editor.SelectionState;

        if (!selection.IsEmpty)
        {
            var range = selection.Range;
            var copy = text.Substring(range.Start, range.Length);
            return Apply(snapshot, [new RichTextEdit(new RichTextRange(range.End, 0), copy)],
                new RichTextSelectionState(range.End, range.End + copy.Length), "Dupliquer") || true;
        }

        var lineStart = LineStart(text, selection.Active);
        var lineEnd = LineEnd(text, selection.Active);
        var line = text[lineStart..lineEnd];
        return Apply(snapshot, [new RichTextEdit(new RichTextRange(lineEnd, 0), "\n" + line)], selection.Active + line.Length + 1, "Dupliquer") || true;
    }

    /// <summary>Ctrl+K, Ctrl+C / Ctrl+K, Ctrl+U : prefixe pose au niveau d'indentation minimal des lignes non vides.</summary>
    private void SetLineComments(bool comment)
    {
        var prefix = CommentPrefix;
        if (string.IsNullOrEmpty(prefix))
            return;

        var snapshot = _editor.Document.CurrentSnapshot;
        var text = snapshot.Text;
        var selection = _editor.SelectionState;
        var (start, end) = SelectedLines(text, selection.Range);

        List<(int Start, int End)> lines = [];
        for (var lineStart = start; ; lineStart = LineEnd(text, lineStart) + 1)
        {
            var lineEnd = LineEnd(text, lineStart);
            lines.Add((lineStart, lineEnd));
            if (lineEnd >= end)
                break;
        }

        List<RichTextEdit> edits = [];
        if (comment)
        {
            var filled = lines.Where(l => LeadingWhitespaceLength(text, l.Start, l.End) < l.End - l.Start).ToList();
            if (filled.Count == 0)
                return;

            var indent = filled.Min(l => LeadingWhitespaceLength(text, l.Start, l.End));
            edits.AddRange(filled.Select(l => new RichTextEdit(new RichTextRange(l.Start + indent, 0), prefix)));
        }
        else
        {
            foreach (var (lineStart, lineEnd) in lines)
            {
                var at = lineStart + LeadingWhitespaceLength(text, lineStart, lineEnd);
                if (text.AsSpan(at, lineEnd - at).StartsWith(prefix, StringComparison.Ordinal))
                    edits.Add(new RichTextEdit(new RichTextRange(at, prefix.Length), string.Empty));
            }
        }

        if (edits.Count == 0)
            return;

        var mapped = new RichTextSelectionState(MapOffset(selection.Anchor, edits), MapOffset(selection.Active, edits));
        Apply(snapshot, edits, mapped, comment ? "Commenter" : "Décommenter");
    }

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
            var lineStart = LineStart(text, caret);
            var lineEnd = LineEnd(text, caret);
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

            var lineStart = LineStart(snapshot.Text, selection.Active);
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
        if (!Pairs.ContainsKey(typed) && typed is not (')' or ']' or '}'))
            return;

        var start = change.OldRange.Start;
        var replaced = previous.Substring(start, change.OldRange.Length);
        var revision = _editor.Document.Revision;

        // On ne modifie pas le document pendant qu'il notifie sa propre modification.
        _editor.Dispatcher.Dispatch(() => OnTyped(revision, typed, start, replaced, previous));
    }

    private void OnTyped(RichTextRevision revision, char typed, int start, string replaced, string previous)
    {
        if (_isDisposed || _editor.IsReadOnly || _editor.Document.Revision != revision)
            return;

        var snapshot = _editor.Document.CurrentSnapshot;
        var text = snapshot.Text;
        var caret = start + 1;
        var next = caret < text.Length ? text[caret] : '\0';

        // Pas de paire dans un commentaire ni dans une chaine.
        var region = SourceScanner.RegionAt(previous, start, Language);

        // Selection entouree par la paire, la selection reste sur le texte entoure.
        if (replaced.Length > 0)
        {
            if (region == SourceRegion.Code && Pairs.TryGetValue(typed, out var closing))
                ApplyTyping(snapshot, new RichTextEdit(new RichTextRange(start, 1), typed + replaced + closing),
                    new RichTextSelectionState(caret, caret + replaced.Length));
            return;
        }

        // Fermant deja present : il est saute.
        var isQuote = typed is '"' or '\'';
        if (next == typed && (isQuote ? region == SourceRegion.String : region == SourceRegion.Code && typed is ')' or ']' or '}'))
        {
            ApplyTyping(snapshot, new RichTextEdit(new RichTextRange(caret, 1), string.Empty), new RichTextSelectionState(caret, caret));
            return;
        }

        if (region != SourceRegion.Code)
            return;

        if (typed == '}')
        {
            Outdent(snapshot, start);
            return;
        }

        if (!Pairs.TryGetValue(typed, out var close))
            return;

        // Pas de paire collee a un mot (it's, l'apostrophe) ni avant du texte.
        if (isQuote && start > 0 && char.IsLetterOrDigit(text[start - 1]))
            return;

        if (next != '\0' && !char.IsWhiteSpace(next) && next is not (')' or ']' or '}' or ';' or ','))
            return;

        ApplyTyping(snapshot, new RichTextEdit(new RichTextRange(caret, 0), close.ToString()), new RichTextSelectionState(caret, caret));
    }

    /// <summary>} tape sur une ligne vide : la ligne perd un niveau d'indentation.</summary>
    private void Outdent(CodeDocumentSnapshot snapshot, int bracePosition)
    {
        var text = snapshot.Text;
        var lineStart = LineStart(text, bracePosition);
        var indent = bracePosition - lineStart;
        if (indent == 0 || LeadingWhitespaceLength(text, lineStart, bracePosition) != indent)
            return;

        var count = 0;
        if (text[bracePosition - 1] == '\t')
        {
            count = 1;
        }
        else
        {
            while (count < _editor.IndentSize && count < indent && text[bracePosition - 1 - count] == ' ')
                count++;
        }

        if (count == 0)
            return;

        var caret = bracePosition + 1 - count;
        ApplyTyping(snapshot, new RichTextEdit(new RichTextRange(bracePosition - count, count), string.Empty), new RichTextSelectionState(caret, caret));
    }

    private void ApplyTyping(CodeDocumentSnapshot snapshot, RichTextEdit edit, RichTextSelectionState selection)
        => _editor.TryApplyEdits(snapshot.Revision, [edit], selection, TypingOptions);

    // ---------- Outils ----------

    private bool Apply(CodeDocumentSnapshot snapshot, IReadOnlyList<RichTextEdit> edits, int caret, string description)
        => Apply(snapshot, edits, new RichTextSelectionState(caret, caret), description);

    private bool Apply(CodeDocumentSnapshot snapshot, IReadOnlyList<RichTextEdit> edits, RichTextSelectionState selection, string description)
    {
        var applied = _editor.TryApplyEdits(snapshot.Revision, edits, selection, new RichTextEditOptions(RichTextUndoBehavior.CreateUnit, description, null));
        if (applied)
            _editor.ScrollIntoView(new RichTextRange(selection.Active, 0));
        return applied;
    }

    private static RichTextRange FromBounds(int start, int end) => new(start, end - start);

    private static int LineStart(string text, int position)
    {
        var index = position > 0 ? text.LastIndexOf('\n', Math.Min(position, text.Length) - 1) : -1;
        return index + 1;
    }

    private static int LineEnd(string text, int position)
    {
        var index = position < text.Length ? text.IndexOf('\n', position) : -1;
        return index < 0 ? text.Length : index;
    }

    private static int LeadingWhitespaceLength(string text, int start, int end)
    {
        var index = start;
        while (index < end && text[index] is ' ' or '\t')
            index++;
        return index - start;
    }

    /// <summary>Colonne affichee : une tabulation va au taquet suivant.</summary>
    private static int VisualColumn(string text, int lineStart, int position, int size)
    {
        var column = 0;
        for (var i = lineStart; i < position; i++)
            column = text[i] == '\t' ? (column / size + 1) * size : column + 1;
        return column;
    }

    /// <summary>Lignes couvertes par la selection ; une selection finissant en debut de ligne n'inclut pas cette ligne.</summary>
    private static (int Start, int End) SelectedLines(string text, RichTextRange range)
    {
        var end = range.End;
        if (range.Length > 0 && text[end - 1] == '\n')
            end--;

        return (LineStart(text, range.Start), LineEnd(text, Math.Max(end, range.Start)));
    }

    /// <summary>Position apres des modifications exprimees dans les coordonnees d'origine (ordonnees, sans chevauchement).</summary>
    private static int MapOffset(int offset, IEnumerable<RichTextEdit> edits)
    {
        var shift = 0;
        foreach (var edit in edits)
        {
            var range = edit.Range;
            if (range.End <= offset)
                shift += edit.Text.Length - range.Length;
            else if (range.Start < offset)
                shift += range.Start + edit.Text.Length - offset;
        }

        return offset + shift;
    }
}
