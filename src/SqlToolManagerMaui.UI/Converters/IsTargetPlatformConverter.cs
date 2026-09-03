using System.Globalization;

namespace SqlToolManagerMaui.UI.Converters;

public class IsTargetPlatformConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var target = (parameter ?? string.Empty).ToString()!.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(target))
        {
            return false;
        }

        switch (target)
        {
            case "ios":
            case "apple":
                return DeviceInfo.Platform == DevicePlatform.iOS;
            case "android":
                return DeviceInfo.Platform == DevicePlatform.Android;
            case "maccatalyst":
            case "mac":
            case "macos":
                return DeviceInfo.Platform == DevicePlatform.MacCatalyst;
            case "windows":
            case "winui":
                return DeviceInfo.Platform == DevicePlatform.WinUI;
            default:
                // fallback : compare string names
                return string.Equals(DeviceInfo.Platform.ToString(), target, StringComparison.OrdinalIgnoreCase);
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}