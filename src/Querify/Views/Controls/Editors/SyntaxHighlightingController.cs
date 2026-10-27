using CodeEdit.Maui;
using RichEdit.Maui;
using Quill.Code.Highlighting;
using Querify.UI.Extensions;
using System.Collections.Concurrent;
using NativeCodeEditor = CodeEdit.Maui.CodeEditor;

namespace Querify.Views.Controls.Editors;

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
    private readonly RichTextDecorationLayer _layer;

    private SyntaxDefinition? _definition;
    private CancellationTokenSource? _pending;
    private bool _isDisposed;

    public SyntaxHighlightingController(NativeCodeEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        _editor = editor;
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
        HighlightAsync(_editor.Document.CurrentSnapshot, cancellation.Token).FireAndForget("Coloration syntaxique en échec.");
    }

    private async Task HighlightAsync(CodeDocumentSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(DebounceDelay, cancellationToken);

            var definition = _definition!;

            // Tokenisation ET construction des decorations hors du thread UI : seul TrySet y reste.
            var decorations = await Task.Run(() =>
            {
                var text = snapshot.Text;
                var tokens = Quill.Code.Highlighting.SyntaxHighlighter.Highlight(definition, text, cancellationToken);
                return BuildDecorations(tokens, text.Length, definition.DefaultColor?.Foreground);
            }, cancellationToken);

            _editor.Dispatcher.Dispatch(() =>
            {
                if (cancellationToken.IsCancellationRequested || _isDisposed)
                    return;

                // Revision perimee (frappe pendant l'analyse) : on relance sur le texte courant
                // plutot que de laisser les anciennes decorations, etirees par l'edition.
                var applied = _layer.TrySet(snapshot.Revision, decorations);

                if (!applied && _editor.Document.Revision != snapshot.Revision)
                    Refresh();
            });
        }
        catch (OperationCanceledException)
        {
            // Passe remplacee par une plus recente.
        }
    }

    /// <summary>
    /// Le texte non reconnu recoit aussi une decoration (couleur par defaut) : sans elle, un caractere
    /// tape apres un commentaire herite du vert du natif, et CodeEdit ne le repeint jamais faute de
    /// difference dans sa projection.
    /// </summary>
    private static RichTextDecoration[] BuildDecorations(IReadOnlyList<SyntaxToken> tokens, int length, string? defaultColor)
    {
        List<RichTextDecoration> decorations = new(tokens.Count * 2 + 1);
        var position = 0;

        foreach (var token in tokens)
        {
            if (defaultColor is not null && token.Start > position)
                decorations.Add(Decorate(position, token.Start - position, defaultColor));

            // Regle sans couleur : texte reconnu, laisse a la couleur par defaut.
            if ((token.Color?.Foreground ?? defaultColor) is { } color)
                decorations.Add(Decorate(token.Start, token.Length, color));

            position = token.End;
        }

        if (defaultColor is not null && length > position)
            decorations.Add(Decorate(position, length - position, defaultColor));

        return [.. decorations];
    }

    /// <summary>Une poignee de couleurs par langage : parsees une fois, pas a chaque token.</summary>
    private static readonly ConcurrentDictionary<string, Color> Colors = new(StringComparer.OrdinalIgnoreCase);

    private static RichTextDecoration Decorate(int start, int length, string color)
        => new(new RichTextRange(start, length), new RichTextDecorationStyle { ForegroundColor = Colors.GetOrAdd(color, Color.FromArgb) });

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
