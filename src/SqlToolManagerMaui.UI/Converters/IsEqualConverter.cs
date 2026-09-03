using System.Globalization;

namespace SqlToolManagerMaui.UI.Converters;

public class IsEqualConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is Binding binding
            && binding.Source is Page page)
        {
            var bindingContext = page.BindingContext;
            parameter = bindingContext?.GetType().GetProperty(binding.Path)?.GetValue(bindingContext) ?? false;
        }

        string a = value?.ToString() ?? string.Empty;
        string b = parameter?.ToString() ?? string.Empty;
        return a.Equals(b);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
