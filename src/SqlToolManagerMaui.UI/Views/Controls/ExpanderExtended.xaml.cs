using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;

namespace SqlToolManagerMaui.UI.Views.Controls;

public partial class ExpanderExtended : Expander
{
    /// <summary>Texte de l'en-tete.</summary>
    [BindableProperty]
    public partial string Title { get; set; } = string.Empty;

    /// <summary>Arrondi des coins (fond et contenu sont decoupes).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnCornerRadiusChanged))]
    public partial CornerRadius CornerRadius { get; set; }

    public ExpanderExtended()
    {
        InitializeComponent();
    }

    private static void OnCornerRadiusChanged(BindableObject bindable, object oldValue, object newValue)
        => ((ExpanderExtended)bindable).UpdateClip();

    // L'Expander n'a ni StrokeShape ni CornerRadius : l'arrondi passe par un Clip a la taille du controle.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        UpdateClip();
    }

    private void UpdateClip()
    {
        if (Width <= 0 || Height <= 0 || CornerRadius == default)
        {
            Clip = null;
            return;
        }

        Clip = new RoundRectangleGeometry(CornerRadius, new Rect(0, 0, Width, Height));
    }
}
