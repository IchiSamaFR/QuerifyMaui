using CommunityToolkit.Maui;
using Microsoft.Maui.Converters;
using Querify.UI.Inputs.Core;
using System.ComponentModel;
using System.Windows.Input;

namespace Querify.UI.Inputs;

public partial class TextField
{
    // ── Bindable Properties ──────────────────────────────────────────────────────────────────────

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
    public partial ClearButtonVisibility ClearButtonVisibility { get; set; } = ClearButtonVisibility.WhileEditing;

    [BindableProperty]
    public partial bool IsPassword { get; set; }

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial ICommand? ReturnCommand { get; set; }

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial object? ReturnCommandParameter { get; set; }

    [BindableProperty]
    public partial ReturnType ReturnType { get; set; } = ReturnType.Default;

    [BindableProperty]
    public partial double CharacterSpacing { get; set; }

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial int SelectionLength { get; set; }

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial int CursorPosition { get; set; }

    [BindableProperty]
    public partial bool IsTextPredictionEnabled { get; set; } = true;

    [BindableProperty]
    public new partial bool IsReadOnly { get; set; }

    [BindableProperty]
    public partial int MaxLength { get; set; } = int.MaxValue;

    [BindableProperty]
    public partial TextAlignment HorizontalTextAlignment { get; set; } = TextAlignment.Start;

    [BindableProperty]
    public partial TextAlignment VerticalTextAlignment { get; set; } = TextAlignment.Center;

    [BindableProperty]
    public partial Color? SelectionHighlightColor { get; set; }

    [BindableProperty]
    public partial Color? CursorColor { get; set; }

    [BindableProperty]
    public partial bool DisallowClearButtonFocus { get; set; }

    [BindableProperty]
    public partial bool SelectAllTextOnFocus { get; set; }
}
