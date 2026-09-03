using SqlToolManagerMaui.UI.Inputs.Core;

namespace SqlToolManagerMaui.UI.Behaviors;

/// <summary>
/// Variante de <see cref="DataAnnotationsBehavior"/> activable/désactivable,
/// ciblant une propriété par son nom. Standalone (sans InputKit / UraniumUI).
/// </summary>
public class ToggleableDataAnnotationsBehavior : Behavior<View>
{
    protected View? AssociatedObject { get; private set; }

    public static readonly BindableProperty IsEnabledProperty =
        BindableProperty.Create(nameof(IsEnabled),
            typeof(bool),
            typeof(ToggleableDataAnnotationsBehavior),
            true,
            BindingMode.OneWay,
            propertyChanged: OnBindPropertyChanged);

    public bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public static readonly BindableProperty PropertyNameProperty =
        BindableProperty.Create(
            nameof(PropertyName),
            typeof(string),
            typeof(ToggleableDataAnnotationsBehavior),
            null,
            BindingMode.OneWay,
            propertyChanged: OnBindPropertyChanged);

    public string? PropertyName
    {
        get => (string?)GetValue(PropertyNameProperty);
        set => SetValue(PropertyNameProperty, value);
    }

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
