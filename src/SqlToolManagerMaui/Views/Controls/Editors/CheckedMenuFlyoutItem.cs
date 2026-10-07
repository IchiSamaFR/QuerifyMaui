using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Entree de menu "cochable" : le libelle reste fixe et une pastille apparait devant lui
/// quand l'option est active.
/// MenuFlyoutItem n'a pas de contenu templatable (sous Windows c'est un MenuFlyoutItem WinUI
/// natif) : la pastille passe par la colonne d'icone du natif, posee meme decochee pour que
/// les libelles restent alignes d'une entree a l'autre.
/// </summary>
public partial class CheckedMenuFlyoutItem : MenuFlyoutItem
{
    /// <summary>Glyphe affiche devant le libelle lorsque l'entree est cochee.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnStateChanged))]
    public partial string? Prefix { get; set; } = "●";

    /// <summary>Libelle de l'entree, invariant : c'est la pastille qui porte l'etat.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnLabelChanged))]
    public partial string? Label { get; set; } = string.Empty;

    /// <summary>Etat courant de l'option.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnStateChanged))]
    public partial bool IsChecked { get; set; }

    private static void OnLabelChanged(BindableObject bindable, object oldValue, object newValue)
        => ((MenuFlyoutItem)bindable).Text = newValue as string ?? string.Empty;

    private static void OnStateChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CheckedMenuFlyoutItem)bindable).UpdateBadge();

    private void UpdateBadge()
    {
#if WINDOWS
        if (Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.MenuFlyoutItem native)
            return;

        // L'icone est posee meme decochee (opacite nulle) : sans elle la colonne
        // disparaitrait et le libelle se decalerait d'une entree a l'autre.
        if (native.Icon is not Microsoft.UI.Xaml.Controls.FontIcon icon)
        {
            icon = new Microsoft.UI.Xaml.Controls.FontIcon
            {
                // Segoe Fluent Icons ne couvre pas U+25CF : on force une police de texte.
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe UI"),
                // Le template WinUI place l'icone dans un Viewbox fige a 16x16 : la taille
                // effective du glyphe se regle ici, l'ecart avant le libelle dans
                // Platforms/Windows/App.xaml.cs (MenuFlyoutItemPlaceholderThemeThickness).
                FontSize = 8d,
            };
            native.Icon = icon;
        }

        icon.Glyph = Prefix ?? string.Empty;
        icon.Opacity = IsChecked ? 1d : 0d;
#endif
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        // Le natif n'existe qu'a partir d'ici : l'etat accumule avant est pousse maintenant.
        UpdateBadge();
    }
}
