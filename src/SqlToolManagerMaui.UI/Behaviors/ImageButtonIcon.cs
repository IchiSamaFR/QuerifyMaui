using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SqlToolManagerMaui.UI.Behaviors;

// SqlToolManagerMaui.UI
public static class ImageButtonIcon
{
    public static readonly BindableProperty DisabledColorProperty =
        BindableProperty.CreateAttached("DisabledColor", typeof(Color), typeof(ImageButtonIcon), null,
            propertyChanged: OnDisabledColorChanged);

    public static Color? GetDisabledColor(BindableObject b) => (Color?)b.GetValue(DisabledColorProperty);
    public static void SetDisabledColor(BindableObject b, Color? v) => b.SetValue(DisabledColorProperty, v);

    static readonly BindableProperty OriginalColorProperty =
        BindableProperty.CreateAttached("OriginalColor", typeof(Color), typeof(ImageButtonIcon), null);

    static void OnDisabledColorChanged(BindableObject b, object? _, object? n)
    {
        if (b is not ImageButton btn) return;
        btn.PropertyChanged -= OnPropertyChanged;
        if (n is not null) { btn.PropertyChanged += OnPropertyChanged; Apply(btn); }
    }

    static void OnPropertyChanged(object? s, PropertyChangedEventArgs e)
    {
        if (s is ImageButton btn && e.PropertyName == nameof(ImageButton.IsEnabled)) Apply(btn);
    }

    static void Apply(ImageButton btn)
    {
        if (btn.Source is not FontImageSource fis) return;
        if (!btn.IsEnabled)
        {
            if (btn.GetValue(OriginalColorProperty) is null) btn.SetValue(OriginalColorProperty, fis.Color);
            fis.Color = GetDisabledColor(btn);
        }
        else if (btn.GetValue(OriginalColorProperty) is Color c)
        {
            fis.Color = c;
            btn.ClearValue(OriginalColorProperty);
        }
    }
}