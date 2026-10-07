using CommunityToolkit.Maui;
using SqlToolManagerMaui.UI.Inputs.Core;

namespace SqlToolManagerMaui.UI.Behaviors;

/// <summary>
/// Variante de <see cref="DataAnnotationsBehavior"/> activable/désactivable,
/// ciblant une propriété par son nom. Standalone (sans InputKit / UraniumUI).
/// </summary>
public partial class ToggleableDataAnnotationsBehavior : Behavior<View>
{
    protected View? AssociatedObject { get; private set; }

    [BindableProperty(DefaultBindingMode = BindingMode.OneWay, PropertyChangedMethodName = nameof(OnBindPropertyChanged))]
    public partial bool IsEnabled { get; set; } = true;

    [BindableProperty(DefaultBindingMode = BindingMode.OneWay, PropertyChangedMethodName = nameof(OnBindPropertyChanged))]
    public partial string? PropertyName { get; set; }

    private static void OnBindPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var behavior = (ToggleableDataAnnotationsBehavior)bindable;
        if (behavior.AssociatedObject is null)
            return;

        behavior.ApplyAnnotations();
    }

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        AssociatedObject = bindable;
        bindable.BindingContextChanged += OnViewBindingContextChanged;

        // Permet de binder IsEnabled / PropertyName sur le BindingContext de la vue.
        SetBinding(BindingContextProperty, new Binding(nameof(BindingContext), source: bindable));

        ApplyAnnotations();
    }

    protected override void OnDetachingFrom(View bindable)
    {
        bindable.BindingContextChanged -= OnViewBindingContextChanged;
        RemoveBinding(BindingContextProperty);
        AssociatedObject = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnViewBindingContextChanged(object? sender, EventArgs e) => ApplyAnnotations();

    private void ApplyAnnotations()
    {
        if (AssociatedObject is not IValidatable validatable)
            return;

        validatable.Validations.Clear();
        validatable.DisplayValidation();

        if (!IsEnabled || string.IsNullOrEmpty(PropertyName))
            return;

        var context = AssociatedObject?.BindingContext;
        if (context is null)
            return;

        var propertyInfo = DataAnnotationsBehavior.ResolveProperty(context.GetType(), PropertyName);
        if (propertyInfo is null)
            return;

        DataAnnotationsBehavior.AddValidations(validatable, propertyInfo);
        validatable.DisplayValidation();
    }
}
