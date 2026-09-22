using CodeEdit.Maui;
using RichEdit.Maui;
using SqlToolManagerMaui.Models.Editors.Highlighting;
using SqlToolManagerMaui.Services.Editors.Highlighting;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Branche une definition .xshd sur un <see cref="CodeEditor"/>. CodeEdit.Maui ne colorie rien
/// de lui-meme : la coloration se publie dans une couche de decorations, calculee hors du
/// thread UI puis appliquee sur la revision du snapshot analyse.
/// </summary>
public sealed class SyntaxHighlightingController : IDisposable
{
    /// <summary>Laisse passer une rafale de frappe avant de re-tokeniser tout le document.</summary>
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(150);

    private readonly CodeEditor _editor;
    private readonly ISyntaxDefinitionStore _definitions;
    private readonly ISyntaxHighlighter _highlighter;
    private readonly RichTextDecorationLayer _layer;

    private SyntaxDefinition? _definition;
    private CancellationTokenSource? _pending;
    private bool _isDisposed;

    public SyntaxHighlightingController(CodeEditor editor, ISyntaxDefinitionStore definitions, ISyntaxHighlighter highlighter)
    {
        ArgumentNullException.ThrowIfNull(editor);

        _editor = editor;
        _definitions = definitions;
        _highlighter = highlighter;
        _layer = editor.Decorations.CreateLayer();

        _editor.ContentChanged += OnEditorChanged;
        _editor.DocumentChanged += OnEditorChanged;
    }

    /// <summary>
    /// Charge le langage puis colorie. Le chargement de l'asset est asynchrone : l'editeur
    /// reste en noir et blanc pendant ce court instant.
    /// </summary>
    public void Start(string language)
    {
        _ = StartAsync(language);
    }

    private async Task StartAsync(string language)
    {
        _definition = await _definitions.GetAsync(language);

        if (!_isDisposed && _definition is not null)
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
