using MauiIcons.Fluent;
using SqlToolManagerMaui.UI.Tools;

namespace SqlToolManagerMaui.UI.MarkupExtensions;

/// <summary>
/// Produit un <see cref="FontImageSource"/> Fluent Filled utilisable dans un <c>Setter</c> de style,
/// là où l'extension <c>{mk:FluentFilledImage}</c> de MauiIcons lève une exception.
/// </summary>
[AcceptEmptyServiceProvider]
public sealed class FluentImageExtension : IMarkupExtension<ImageSource>
{
    /// <summary>Icône à afficher.</summary>
    public FluentIcons Icon { get; set; }

    /// <summary>Taille de l'icône (20 par défaut).</summary>
    public double IconSize { get; set; } = 20d;

    /// <summary>Couleur de l'icône ; à défaut, noir ou blanc selon le thème courant.</summary>
    public Color? IconColor { get; set; }

    public ImageSource ProvideValue(IServiceProvider serviceProvider)
        => new FontImageSource
        {
            FontFamily = GlyphIconHelper.FontFamily,
            Glyph = GlyphIconHelper.Glyph(Icon),
            Size = IconSize,
            Color = IconColor ?? (Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.White : Colors.Black),
        };

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
