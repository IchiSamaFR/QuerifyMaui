using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Shapes;
using SqlToolManagerMaui.UI.Inputs;
using SqlToolManagerMaui.UI.Inputs.Core;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace SqlToolManagerMaui.UI.Attachments;

/// <summary>
/// Œil show/hide ajouté en fin de TextField pour basculer IsPassword.
/// Réimplémentation standalone (sans UraniumUI StatefulContentView).
/// </summary>
public partial class TextFieldPasswordShowHideAttachment : ContentView
{
    [BindableProperty(DefaultBindingMode = BindingMode.OneWay, PropertyChangedMethodName = nameof(OnIconColorChanged))]
    public partial Color? IconColor { get; set; } = Colors.DarkGray;

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

    private static void OnIconColorChanged(BindableObject bindable, object oldValue, object newValue)
        => ((TextFieldPasswordShowHideAttachment)bindable).UpdateIcon();

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
