using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.UI.Views.Controls.Layouts;

/// <summary>
/// Séparateur visuel horizontal ou vertical entre des listes de boutons / controls.
/// La couleur se définit via <see cref="VisualElement.BackgroundColor"/>.
/// </summary>
public partial class Separator : ContentView
{
    /// <summary>Orientation de la ligne : Horizontal (trait horizontal) ou Vertical (trait vertical).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnLayoutChanged))]
    public partial StackOrientation Orientation { get; set; } = StackOrientation.Horizontal;

    /// <summary>Épaisseur du trait.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnLayoutChanged))]
    public partial double Thickness { get; set; } = 1d;

    public Separator()
    {
        InitializeComponent();
        ApplyLayout();
    }

    private static void OnLayoutChanged(BindableObject bindable, object oldValue, object newValue)
        => ((Separator)bindable).ApplyLayout();

    private void ApplyLayout()
    {
        if (Orientation == StackOrientation.Horizontal)
        {
            HeightRequest = Thickness;
            WidthRequest = -1;
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Center;
        }
        else
        {
            WidthRequest = Thickness;
            HeightRequest = -1;
            VerticalOptions = LayoutOptions.Fill;
            HorizontalOptions = LayoutOptions.Center;
        }
    }
}