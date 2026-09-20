namespace SqlToolManagerMaui.UI.Inputs;

/// <summary>
/// Variante de <see cref="ButtonView"/> au contenu figé : une icône puis un texte,
/// disposés horizontalement. Chaque partie disparaît quand sa valeur est nulle / vide.
///
/// Exemple :
/// <![CDATA[
///   <inp:IconButtonView Text="Enregistrer"
///                       Icon="save.png"
///                       Command="{Binding SaveCommand}" />
/// ]]>
/// </summary>
[ContentProperty(nameof(Children))]
public partial class IconButtonView : ButtonView
{
    protected override Layout _layout { get; } = new HorizontalStackLayout
    {
        VerticalOptions = LayoutOptions.Fill,
        HorizontalOptions = LayoutOptions.Fill,
    };

    private readonly Lazy<Image> _iconView = new (() => new Image
    {
        StyleClass = new[] { "InputField.Icon" },
        HorizontalOptions = LayoutOptions.Start,
        VerticalOptions = LayoutOptions.Center,
    });

    private readonly Label _labelView = new()
    {
        VerticalOptions = LayoutOptions.Center,
        VerticalTextAlignment = TextAlignment.Center,
        HorizontalOptions = LayoutOptions.Fill,
        IsVisible = false,
    };

    public IconButtonView() : base()
    {
        _labelView.SetBinding(Label.TextProperty, new Binding(nameof(Text), source: this));
        _labelView.SetBinding(Label.TextColorProperty, new Binding(nameof(TextColor), source: this));
        _labelView.SetBinding(Label.FontSizeProperty, new Binding(nameof(FontSize), source: this));
        _labelView.SetBinding(Label.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));
        _labelView.SetBinding(Label.FontAttributesProperty, new Binding(nameof(FontAttributes), source: this));

        _layout.SetBinding(HorizontalStackLayout.SpacingProperty, new Binding(nameof(Spacing), source: this));

        _layout.Children.Add(_iconView.Value);
        _layout.Children.Add(_labelView);
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            // Une partie sans valeur ne doit pas occuper d'espace, ni laisser
            // le Spacing du stack derrière elle.
            case nameof(Icon):
            case nameof(IconSize):
            case nameof(TextColor):
                UpdateIcon();
                break;
            case nameof(Text):
                _labelView.IsVisible = !string.IsNullOrEmpty(Text);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// La source est affectée directement plutôt que bindée : une ImageSource est un Element,
    /// et la ré-affecter via un binding la laisse parfois sans parent (rien n'est rendu).
    /// </summary>
    private void UpdateIcon()
    {
        _iconView.Value.Source = Icon;
        _iconView.Value.IsVisible = Icon is not null;

        // IconSize <= 0 : on laisse la source décider (une FontImageSource porte déjà sa taille,
        // par exemple celle passée à {mi:MauiIcon IconSize=20}).
        if (Icon is FontImageSource font)
        {
            if (IconSize > 0)
                font.Size = IconSize;

            // L'icône suit la couleur du texte. Une icône bitmap n'est pas recolorable :
            // seule une FontImageSource (celle produite par {mi:MauiIcon}) l'est.
            if (TextColor is not null)
                font.Color = TextColor;
        }

        _iconView.Value.WidthRequest = IconSize > 0 ? IconSize : -1;
        _iconView.Value.HeightRequest = IconSize > 0 ? IconSize : -1;
    }
}
