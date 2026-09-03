using Microsoft.Maui.Controls.Shapes;
using SqlToolManagerMaui.UI.Inputs;
using SqlToolManagerMaui.UI.Inputs.Core;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace SqlToolManagerMaui.UI.Attachments;

/// <summary>
/// Œil show/hide ajouté en fin de TextField pour basculer IsPassword.
/// Réimplémentation standalone (sans UraniumUI StatefulContentView).
/// </summary>
public class TextFieldPasswordShowHideAttachment : ContentView
{
    public static readonly BindableProperty IconColorProperty =
        BindableProperty.Create(nameof(IconColor),
            typeof(Color),
            typeof(TextFieldPasswordShowHideAttachment),
            Colors.DarkGray,
            BindingMode.OneWay,
            propertyChanged: (bindable, _, _) => ((TextFieldPasswordShowHideAttachment)bindable).UpdateIcon());

    public Color? IconColor
    {
        get => (Color?)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public TextField? TextField { get; protected set; }

    public TextFieldPasswordShowHideAttachment()
    {
        VerticalOptions = LayoutOptions.Center;
        Padding = new Thickness(5, 0);
        Margin = new Thickness(0, 0, 5, 0);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => SwitchPassword();
        GestureRecognizers.Add(tap);
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();

        TextField = FindInParents<TextField>();
        if (TextField is not null)
            UpdateIcon();
    }

    private T? FindInParents<T>() where T : Element
    {
        Element? current = Parent;
        while (current is not null)
        {
            if (current is T match)
                return match;
            current = current.Parent;
        }

        return null;
    }

    protected virtual void SwitchPassword()
    {
        if (TextField is null)
        {
            UpdateIcon();
            return;
        }

        TextField.IsPassword = !TextField.IsPassword;
        UpdateIcon();
    }

    protected void UpdateIcon()
    {
        Content = TextField is null
            ? null
            : GetPathFromData(TextField.IsPassword ? InputShapes.Eye : InputShapes.EyeSlash);
    }

    private Path GetPathFromData(Geometry data)
    {
        return new Path
        {
            Fill = IconColor
                   ?? InputColors.Get("OnBackground", "OnBackgroundDark", Colors.DarkGray).WithAlpha(0.5f),
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            WidthRequest = 20,
            HeightRequest = 20,
            Aspect = Stretch.Uniform,
            Data = data,
        };
    }
}
