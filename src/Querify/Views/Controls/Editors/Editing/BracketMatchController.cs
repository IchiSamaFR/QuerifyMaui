using CodeEdit.Maui;
using Querify.Core.Enums.Editors.Highlighting;
using Quill.Code.Editing;
using RichEdit.Maui;
using NativeCodeEditor = CodeEdit.Maui.CodeEditor;

namespace Querify.Views.Controls.Editors.Editing;

/// <summary>
/// Curseur contre un crochet : lui et son crochet associe sont encadres d'un fond. Les crochets des chaines
/// et des commentaires sont ignores.
/// </summary>
public sealed class BracketMatchController : IDisposable
{
    private readonly NativeCodeEditor _editor;
    private readonly Func<SyntaxLanguage> _language;
    private readonly RichTextDecorationLayer _layer;
    private readonly RichTextDecorationStyle _style;

    /// <summary>Decoupe du dernier texte analyse : un simple deplacement du curseur ne relit pas le document.</summary>
    private (RichTextRevision Revision, SyntaxLanguage Language, SourceRegion[] Regions)? _scan;

    private bool _isScheduled;
    private bool _isDisposed;

    public BracketMatchController(NativeCodeEditor editor, Func<SyntaxLanguage> language)
    {
        _editor = editor;
        _language = language;
        _layer = editor.Decorations.CreateLayer();
        _style = new RichTextDecorationStyle { BackgroundColor = ResourceColor("Info", Colors.SteelBlue).WithAlpha(0.35f) };

        _editor.SelectionChanged += OnEditorChanged;
        _editor.ContentChanged += OnEditorChanged;
        _editor.DocumentChanged += OnEditorChanged;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _editor.SelectionChanged -= OnEditorChanged;
        _editor.ContentChanged -= OnEditorChanged;
        _editor.DocumentChanged -= OnEditorChanged;
        _layer.Dispose();
    }

    private void OnEditorChanged(object? sender, EventArgs e)
    {
        if (_isScheduled || _isDisposed)
            return;

        _isScheduled = true;
        _editor.Dispatcher.Dispatch(Refresh);
    }

    private void Refresh()
    {
        _isScheduled = false;
        if (_isDisposed)
            return;

        var selection = _editor.SelectionState;
        var snapshot = _editor.Document.CurrentSnapshot;
        if (!selection.IsEmpty || snapshot.Length == 0)
        {
            _layer.Clear();
            return;
        }

        var text = snapshot.Text;
        var language = _language();
        if (_scan is not { } scan || scan.Revision != snapshot.Revision || scan.Language != language)
        {
            scan = (snapshot.Revision, language, SourceScanner.Classify(text, EditorSyntax.For(language)));
            _scan = scan;
        }

        // Comme VS : fermant juste avant le curseur, puis ouvrant juste apres.
        if (BracketMatcher.FindPair(text, selection.Active, scan.Regions) is { } pair)
        {
            _layer.TrySet(snapshot.Revision,
            [
                new RichTextDecoration(new RichTextRange(pair.Open, 1), _style),
                new RichTextDecoration(new RichTextRange(pair.Close, 1), _style),
            ]);
            return;
        }

        _layer.Clear();
    }

    private static Color ResourceColor(string key, Color fallback)
        => Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color ? color : fallback;
}
