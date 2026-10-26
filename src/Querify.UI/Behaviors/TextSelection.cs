namespace Querify.UI.Behaviors;

/// <summary>Rend le texte d'un Label selectionnable a la souris et copiable (Ctrl+C) ; Windows uniquement.</summary>
public static class TextSelection
{
    public static readonly BindableProperty IsEnabledProperty =
        BindableProperty.CreateAttached("IsEnabled", typeof(bool), typeof(TextSelection), false, propertyChanged: OnIsEnabledChanged);

    public static bool GetIsEnabled(BindableObject bindable) => (bool)bindable.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(BindableObject bindable, bool value) => bindable.SetValue(IsEnabledProperty, value);

    static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Label label)
            return;

        // Le TextBlock natif n'existe qu'une fois le handler cree.
        label.HandlerChanged -= OnHandlerChanged;
        label.HandlerChanged += OnHandlerChanged;
        Apply(label);
    }

    static void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (sender is Label label)
            Apply(label);
    }

    static void Apply(Label label)
    {
#if WINDOWS
        if (label.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock textBlock)
            textBlock.IsTextSelectionEnabled = GetIsEnabled(label);
#endif
    }
}
