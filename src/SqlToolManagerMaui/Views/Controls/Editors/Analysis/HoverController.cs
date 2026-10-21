using CodeEdit.Maui;
using RichEdit.Maui;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Editors.Analysis;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Databases.Models.Queries;
using SqlToolManagerMaui.Services.Editors.Analysis;
using SqlToolManagerMaui.UI.Extensions;
using NativeCodeEditor = CodeEdit.Maui.CodeEditor;

namespace SqlToolManagerMaui.Views.Controls.Editors.Analysis;

/// <summary>
/// Infobulle au survol : apres un temps d'arret de la souris sur un nom, affiche sa nature et son type
/// (et les erreurs soulignees a cet endroit) dans <see cref="CodeTooltipPopup"/>. Elle reste ouverte
/// tant que la souris ne quitte pas le nom, et se ferme a la frappe, au clic ou au defilement.
/// </summary>
public sealed class HoverController : IDisposable
{
    private const double Gap = 4;

    /// <summary>Delai de Visual Studio : assez court pour etre utile, assez long pour ne pas suivre la souris qui passe.</summary>
    private static readonly TimeSpan HoverDelay = TimeSpan.FromMilliseconds(450);

    private readonly NativeCodeEditor _editor;
    private readonly CodeTooltipPopup _popup;
    private readonly View _host;
    private readonly ICodeAnalysisService _service;
    private readonly SyntaxLanguage _language;
    private readonly Func<DatabaseConnectionInfo?> _connection;
    private readonly Func<string?> _database;
    private readonly Func<IReadOnlyList<QueryParameter>?> _parameters;
    private readonly DiagnosticsController? _diagnostics;
    private readonly Func<bool> _isSuspended;
    private readonly RichTextLayout _layout;

    private CancellationTokenSource? _pending;

    /// <summary>Plage du nom dont l'infobulle est affichee ; null si fermee.</summary>
    private RichTextRange? _shown;
    private bool _isDisposed;

    /// <param name="host">Conteneur commun a l'editeur et a l'infobulle : repere des coordonnees de l'infobulle.</param>
    /// <param name="diagnostics">Erreurs soulignees, rappelees dans l'infobulle ; null si l'editeur n'en affiche pas.</param>
    /// <param name="isSuspended">Vrai quand une autre liste est ouverte (autocompletion) : pas d'infobulle par-dessus.</param>
    public HoverController(NativeCodeEditor editor, CodeTooltipPopup popup, View host, ICodeAnalysisService service, SyntaxLanguage language,
        Func<DatabaseConnectionInfo?> connection, Func<string?> database, Func<IReadOnlyList<QueryParameter>?> parameters,
        DiagnosticsController? diagnostics, Func<bool> isSuspended)
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
        _diagnostics = diagnostics;
        _isSuspended = isSuspended;
        _layout = editor.TextLayout.CreateRelativeTo(host);

        _editor.PointerMoved += OnPointerMoved;
        _editor.PointerPressed += OnPointerPressed;
        _editor.PointerExited += OnPointerExited;
        _editor.KeyDown += OnKeyDown;
        _editor.ContentChanged += OnContentChanged;
        _editor.DocumentChanged += OnContentChanged;
        _layout.Changed += OnLayoutChanged;
        _popup.SizeChanged += OnPopupSizeChanged;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Hide();

        _editor.PointerMoved -= OnPointerMoved;
        _editor.PointerPressed -= OnPointerPressed;
        _editor.PointerExited -= OnPointerExited;
        _editor.KeyDown -= OnKeyDown;
        _editor.ContentChanged -= OnContentChanged;
        _editor.DocumentChanged -= OnContentChanged;
        _layout.Changed -= OnLayoutChanged;
        _popup.SizeChanged -= OnPopupSizeChanged;
    }

    // ---------- Souris ----------

    private void OnPointerMoved(object? sender, RichTextPointerEventArgs e)
    {
        // Bouton enfonce : l'utilisateur selectionne, il ne lit pas.
        if (e.Buttons != RichTextPointerButtons.None || e.DeviceKind != RichTextPointerDeviceKind.Mouse)
        {
            Hide();
            return;
        }

        // Mise en page propre a l'editeur : meme repere que la position de la souris qu'il rapporte.
        var layout = _editor.TextLayout;
        var hit = layout.HitTest(layout.Capture(), e.Point);

        // Au-dela de la fin d'une ligne, HitTest rend la position la plus proche : il n'y a rien sous la souris.
        if (hit.Kind != RichTextHitKind.Text)
        {
            Hide();
            return;
        }

        if (_shown is { } shown && hit.Position >= shown.Start && hit.Position <= shown.End)
            return;

        Hide();
        Schedule(hit.Position);
    }

    private void OnPointerPressed(object? sender, RichTextPointerEventArgs e) => Hide();

    private void OnPointerExited(object? sender, EventArgs e) => Hide();

    private void OnKeyDown(object? sender, EditorKeyEventArgs e) => Hide();

    private void OnContentChanged(object? sender, EventArgs e) => Hide();

    /// <summary>Defilement ou redimensionnement : l'infobulle suit le nom, et se ferme s'il sort de l'editeur.</summary>
    private void OnLayoutChanged(object? sender, EventArgs e)
    {
        if (_shown is { } shown && _popup.IsVisible)
            Place(shown);
    }

    /// <summary>La taille n'est connue qu'apres mesure : on recale (au-dessus si la place manque en bas).</summary>
    private void OnPopupSizeChanged(object? sender, EventArgs e)
    {
        if (_shown is { } shown && _popup.IsVisible)
            Place(shown);
    }

    // ---------- Infobulle ----------

    private void Schedule(int offset)
    {
        CancelPending();
        var cancellation = new CancellationTokenSource();
        _pending = cancellation;

        ShowAfterDelayAsync(offset, cancellation.Token).FireAndForget("Infobulle indisponible.");
    }

    private async Task ShowAfterDelayAsync(int offset, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(HoverDelay, cancellationToken);

            if (_isDisposed || _isSuspended())
                return;

            var revision = _editor.Document.Revision;
            var diagnostics = _diagnostics?.FindAt(offset) ?? [];
            var request = new CodeAnalysisRequest
            {
                Language = _language,
                Text = _editor.Document.Text,
                Offset = offset,
                Connection = _connection(),
                Database = _database(),
                Parameters = _parameters() ?? [],
            };

            var hover = await _service.GetHoverAsync(request, cancellationToken);

            // Frappe pendant l'analyse : les positions ne correspondent plus au texte.
            if (cancellationToken.IsCancellationRequested || _isDisposed || _editor.Document.Revision != revision)
                return;

            if (hover is null && diagnostics.Count == 0)
                return;

            Show(hover, diagnostics);
        }
        catch (OperationCanceledException)
        {
            // La souris a bouge avant la fin du delai.
        }
    }

    private void Show(HoverInfo? hover, IReadOnlyList<CodeDiagnostic> diagnostics)
    {
        // La plage qui garde l'infobulle ouverte : le nom survole, sinon l'erreur.
        var range = hover is not null
            ? new RichTextRange(hover.Start, hover.Length)
            : new RichTextRange(diagnostics[0].Start, diagnostics[0].Length);

        _popup.Signature = hover?.Signature;
        _popup.Description = hover?.Description;
        _popup.Diagnostics = diagnostics;

        _shown = range;
        _popup.IsVisible = true;
        Place(range);
    }

    /// <summary>Sous le nom, ou au-dessus s'il n'y a pas la place en bas de l'editeur.</summary>
    private void Place(RichTextRange range)
    {
        var bounds = _layout.GetRangeBounds(_layout.Capture(), range);
        if (bounds.Count == 0)
        {
            Hide();
            return;
        }

        var anchor = bounds[0];
        if (anchor.Bottom < 0 || anchor.Top > _host.Height)
        {
            Hide();
            return;
        }

        var height = _popup.Height > 0 ? _popup.Height : 0;
        var width = _popup.Width > 0 ? _popup.Width : CodeTooltipPopup.MaxPopupWidth;

        var y = anchor.Bottom + Gap;
        if (y + height > _host.Height && anchor.Top - height - Gap >= 0)
            y = anchor.Top - height - Gap;

        _popup.TranslationX = Math.Clamp(anchor.Left, 0, Math.Max(0, _host.Width - width));
        _popup.TranslationY = y;
    }

    private void Hide()
    {
        CancelPending();
        _shown = null;

        if (_popup.IsVisible)
            _popup.IsVisible = false;
    }

    private void CancelPending()
    {
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = null;
    }
}
