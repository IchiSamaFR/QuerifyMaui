using CodeEdit.Maui;
using Quill.Code;
using Quill.Code.Analysis;
using RichEdit.Maui;
using Querify.UI.Extensions;
using NativeCodeEditor = CodeEdit.Maui.CodeEditor;

namespace Querify.Views.Controls.Editors.Analysis;

/// <summary>
/// Souligne en vague les erreurs (rouge) et avertissements (jaune) du texte, sur une couche de
/// decorations a part de la coloration. L'analyse part apres une pause de frappe, hors du thread UI.
/// </summary>
public sealed class DiagnosticsController : IDisposable
{
    /// <summary>Plus long que la coloration : une erreur ne doit pas clignoter a chaque lettre d'un mot en cours.</summary>
    private static readonly TimeSpan TypingDelay = TimeSpan.FromMilliseconds(600);

    private readonly NativeCodeEditor _editor;
    private readonly Func<ICodeLanguageService?> _service;
    private readonly RichTextDecorationLayer _layer;
    private readonly Color _errorColor;
    private readonly Color _warningColor;

    private IReadOnlyList<CodeDiagnostic> _diagnostics = [];
    private RichTextRevision? _revision;
    private CancellationTokenSource? _pending;
    private bool _isDisposed;

    /// <param name="service">Service Quill du contexte de l'editeur, lu a chaque analyse ; null : pas d'erreur soulignee.</param>
    public DiagnosticsController(NativeCodeEditor editor, Func<ICodeLanguageService?> service)
    {
        ArgumentNullException.ThrowIfNull(editor);

        _editor = editor;
        _service = service;
        _layer = editor.Decorations.CreateLayer();
        _errorColor = ResourceColor("Danger", Colors.Red);
        _warningColor = ResourceColor("Warning", Colors.Gold);

        _editor.ContentChanged += OnEditorChanged;
        _editor.DocumentChanged += OnEditorChanged;

        Refresh();
    }

    /// <summary>Erreurs a cette position du texte courant ; vide si l'analyse n'a pas encore rattrape la frappe.</summary>
    public IReadOnlyList<CodeDiagnostic> FindAt(int offset)
    {
        if (_revision != _editor.Document.Revision)
            return [];

        return _diagnostics.Where(d => d.Start <= offset && offset <= d.End).ToArray();
    }

    /// <summary>Relance l'analyse : texte modifie, ou contexte change (service recree, connexion ouverte).</summary>
    public void Refresh()
    {
        if (_isDisposed)
            return;

        _pending?.Cancel();
        _pending?.Dispose();

        var cancellation = new CancellationTokenSource();
        _pending = cancellation;

        // Le snapshot se prend sur le thread UI ; l'analyse, elle, part en arriere-plan.
        AnalyzeAsync(_editor.Document.CurrentSnapshot, cancellation.Token).FireAndForget("Analyse des erreurs en échec.");
    }

    private void OnEditorChanged(object? sender, EventArgs e) => Refresh();

    private async Task AnalyzeAsync(CodeDocumentSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TypingDelay, cancellationToken);

            IReadOnlyList<CodeDiagnostic> diagnostics = _service() is { } service
                ? await Task.Run(() => service.GetDiagnosticsAsync(snapshot.Text, cancellationToken), cancellationToken)
                : [];
            var decorations = BuildDecorations(diagnostics, snapshot.Text.Length);

            _editor.Dispatcher.Dispatch(() =>
            {
                if (cancellationToken.IsCancellationRequested || _isDisposed)
                    return;

                // Revision perimee : la frappe a deja relance une analyse, qui posera les siennes.
                if (!_layer.TrySet(snapshot.Revision, decorations))
                    return;

                _diagnostics = diagnostics;
                _revision = snapshot.Revision;
            });
        }
        catch (OperationCanceledException)
        {
            // Analyse remplacee par une plus recente.
        }
    }

    /// <summary>Plages ordonnees et disjointes, comme l'exige la couche : deux erreurs qui se chevauchent n'en font qu'une, rouge si l'une l'est.</summary>
    private RichTextDecoration[] BuildDecorations(IReadOnlyList<CodeDiagnostic> diagnostics, int length)
    {
        List<(int Start, int End, bool IsError)> ranges = [];

        foreach (var diagnostic in diagnostics.OrderBy(d => d.Start))
        {
            var start = Math.Clamp(diagnostic.Start, 0, length);
            var end = Math.Clamp(diagnostic.End, start, length);
            if (end == start)
                continue;

            if (ranges.Count > 0 && start < ranges[^1].End)
            {
                var last = ranges[^1];
                ranges[^1] = (last.Start, Math.Max(last.End, end), last.IsError || diagnostic.IsError);
                continue;
            }

            ranges.Add((start, end, diagnostic.IsError));
        }

        return ranges
            .Select(r => new RichTextDecoration(
                new RichTextRange(r.Start, r.End - r.Start),
                new RichTextDecorationStyle
                {
                    Underline = RichTextUnderlineStyle.Wave,
                    UnderlineColor = r.IsError ? _errorColor : _warningColor,
                }))
            .ToArray();
    }

    private static Color ResourceColor(string key, Color fallback)
        => Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : fallback;

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
