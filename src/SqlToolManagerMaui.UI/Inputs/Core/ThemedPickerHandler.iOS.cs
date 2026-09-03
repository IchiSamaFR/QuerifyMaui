#if IOS
using Microsoft.Maui.Platform;
using UIKit;

namespace SqlToolManagerMaui.UI.Inputs.Core;

public partial class ThemedPickerHandler
{
    protected override void ConnectHandler(MauiPicker platformView)
    {
        base.ConnectHandler(platformView);
        // La "fenêtre" du Picker iOS est la roue (UIPickerView) présentée en bas d'écran.
        if (platformView.InputView is UIPickerView pickerView)
            pickerView.BackgroundColor = ColorFromHex(DialogBackgroundHex);
    }

    private static UIColor ColorFromHex(string hex)
    {
        hex = hex.TrimStart('#');
        var r = Convert.ToInt32(hex.Substring(0, 2), 16);
        var g = Convert.ToInt32(hex.Substring(2, 2), 16);
        var b = Convert.ToInt32(hex.Substring(4, 2), 16);
        return UIColor.FromRGB(r, g, b);
    }
}
#endif