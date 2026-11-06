using CommunityToolkit.Maui;

namespace Querify.UI.Inputs;

public partial class SelectInput
{
    /// <summary>Marge intérieure du texte, appliquée au ComboBox natif (zone cliquable conservée).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnPaddingPropertyChanged))]
    public partial Thickness Padding { get; set; } = new Thickness(8, 0, 8, 0);

    private static void OnPaddingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((SelectInput)bindable).Handler?.UpdateValue(nameof(Padding));
}
