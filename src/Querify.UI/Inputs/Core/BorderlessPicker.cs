namespace Querify.UI.Inputs.Core;

/// <summary>
/// Version "nue" du Picker natif (sans underline Android / bordure iOS),
/// destinée à être habillée par <see cref="InputField"/>.
/// </summary>
public class BorderlessPicker : Picker
{
    static BorderlessPicker() => BorderlessHandlers.Init();

    /// <summary>Marge intérieure du texte dans le contrôle natif (zone cliquable conservée).</summary>
    public static readonly BindableProperty PaddingProperty = BindableProperty.Create(
        nameof(Padding), typeof(Thickness), typeof(BorderlessPicker), new Thickness(8, 0, 8, 0),
        propertyChanged: (bindable, _, _) => ((BorderlessPicker)bindable).Handler?.UpdateValue(nameof(Padding)));

    public Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }
}
