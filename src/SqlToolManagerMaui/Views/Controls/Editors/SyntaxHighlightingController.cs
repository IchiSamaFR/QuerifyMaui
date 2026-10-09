using CodeEdit.Maui;
using RichEdit.Maui;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using NativeCodeEditor = CodeEdit.Maui.CodeEditor;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Publie une coloration sur un editeur natif CodeEdit.Maui. Le controle ne colorie rien
/// de lui-meme : les couleurs passent par une couche de decorations, calculee hors du
/// thread UI puis appliquee sur la revision du snapshot analyse.
/// </summary>
public sealed class SyntaxHighlightingController : IDisposable
{
    /// <summary>Laisse passer une rafale de frappe avant de re-tokeniser tout le document.</summary>
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(150);

    private readonly NativeCodeEditor _editor;
    private readonly ISyntaxHighlighter _highlighter;
    private readonly RichTextDecorationLayer _layer;

    private SyntaxDefinition? _definition;
    private CancellationTokenSource? _pending;
    private bool _isDisposed;

    public SyntaxHighlightingController(NativeCodeEditor editor, ISyntaxHighlighter highlighter)
    {
        ArgumentNullException.ThrowIfNull(editor);

        _editor = editor;
        _highlighter = highlighter;
        _layer = editor.Decorations.CreateLayer();

        _editor.ContentChanged += OnEditorChanged;
        _editor.DocumentChanged += OnEditorChanged;
    }

    /// <summary>Applique une definition et colorie le contenu courant.</summary>
    public void Start(SyntaxDefinition definition)
    {
        _definition = definition;
        Refresh();
    }

    private void OnEditorChanged(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        if (_definition is null || _isDisposed)
            return;

        _pending?.Cancel();
        _pending?.Dispose();

        var cancellation = new CancellationTokenSource();
        _pending = cancellation;

        // Le snapshot se prend sur le thread UI ; l'analyse, elle, part en arriere-plan.
        _ = HighlightAsync(_editor.Document.CurrentSnapshot, cancellation.Token);
    }

    private async Task HighlightAsync(CodeDocumentSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(DebounceDelay, cancellationToken);

            var definition = _definition!;
            var tokens = await Task.Run(() => _highlighter.Tokenize(definition, snapshot.Text, cancellationToken), cancellationToken);

            var decorations = tokens.Select(token => new RichTextDecoration(
                                          new RichTextRange(token.Start, token.Length),
                                          new RichTextDecorationStyle { ForegroundColor = token.Color }))
                                    .ToArray();

            _editor.Dispatcher.Dispatch(() =>
            {
                // TrySet refuse une revision perimee : une frappe pendant l'analyse annule la passe.
                if (!cancellationToken.IsCancellationRequested && !_isDisposed)
                    _layer.TrySet(snapshot.Revision, decorations);
            });
        }
        catch (OperationCanceledException)
        {
            // Passe remplacee par une plus recente.
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _editor.ContentChanged -= OnEditorChanged;
        _editor.DocumentChanged -= OnEditorChanged;

        _pending?.Cancel();
        _pending?.Dispose();
        _layer.Dispose();
    }
}
