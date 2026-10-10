using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.UI.Behaviors;

/// <summary>Place un enfant dans une <see cref="Grid"/> et force le layout quand la position ou la taille change.</summary>
public partial class GridPlacementBehavior : BindableBehavior<View>
{
    [BindableProperty(PropertyChangedMethodName = nameof(OnPlacementChanged))]
    public partial int Column { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnPlacementChanged))]
    public partial int Row { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnPlacementChanged))]
    public partial int ColumnSpan { get; set; } = 1;

    [BindableProperty(PropertyChangedMethodName = nameof(OnPlacementChanged))]
    public partial int RowSpan { get; set; } = 1;

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        Apply();
    }

    private static void OnPlacementChanged(BindableObject bindable, object oldValue, object newValue)
        => ((GridPlacementBehavior)bindable).Apply();

    // Le changement d'une propriete attachee Grid.* par binding ne relance pas toujours le layout
    private void Apply()
    {
        if (AssociatedObject is not { } view)
            return;

        Grid.SetColumn(view, Column);
        Grid.SetRow(view, Row);
        Grid.SetColumnSpan(view, Math.Max(1, ColumnSpan));
        Grid.SetRowSpan(view, Math.Max(1, RowSpan));

        (view.Parent as VisualElement)?.InvalidateMeasure();
        view.InvalidateMeasure();
    }
}
