using CommunityToolkit.Maui;

namespace Querify.UI.Inputs;

public partial class TextInput
{
    /// <summary>Marge intérieure du texte, appliquée au TextBox natif.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnPaddingPropertyChanged))]
    public partial Thickness Padding { get; set; } = new Thickness(8, 0, 8, 0);

    private static void OnPaddingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((TextInput)bindable).Handler?.UpdateValue(nameof(Padding));
}
