using CommunityToolkit.Maui;
using Microsoft.Maui.Converters;
using Querify.UI.Inputs.Core;
using System.ComponentModel;

namespace Querify.UI.Inputs;

public partial class EditorField
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial string Placeholder { get; set; } = string.Empty;

    [BindableProperty]
    public partial Color PlaceholderColor { get; set; } = InputColors.Get("OnBackground", "OnBackgroundDark", Colors.DarkGray);

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial string Text { get; set; } = string.Empty;

    [TypeConverter(typeof(KeyboardTypeConverter))]
    [BindableProperty]
    public partial Keyboard Keyboard { get; set; } = Keyboard.Default;

    [BindableProperty]
    public partial double CharacterSpacing { get; set; }

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial int SelectionLength { get; set; }

    [BindableProperty]
    public partial Color? CursorColor { get; set; }

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial int CursorPosition { get; set; }

    [BindableProperty]
    public partial int MaxLength { get; set; } = int.MaxValue;

    [BindableProperty]
    public partial TextAlignment HorizontalTextAlignment { get; set; } = TextAlignment.Start;

    [BindableProperty]
    public partial TextAlignment VerticalTextAlignment { get; set; } = TextAlignment.Center;

    [BindableProperty]
    public partial bool IsTextPredictionEnabled { get; set; } = true;

    [BindableProperty]
    public partial bool IsSpellCheckEnabled { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnHeightPropertyChanged))]
    public partial double BaseHeightRequest { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnHeightPropertyChanged))]
    public partial double HeightByLines { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnHeightPropertyChanged))]
    public partial double MinimumHeightByLines { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnHeightPropertyChanged))]
    public partial double MaximumHeightByLines { get; set; }

    private static void OnHeightPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((EditorField)bindable).ApplyHeights();
}
