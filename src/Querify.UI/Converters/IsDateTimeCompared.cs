using System.Globalization;

namespace Querify.UI.Converters;

public class IsDateTimeCompared : IMultiValueConverter
{
    public enum ComparisonMode
    {
        None,
        Second,
        Minute,
        Hour,
        Day,
        Month,
        Year
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || !targetType.IsAssignableFrom(typeof(bool)))
        {
            return false;
        }

        if (parameter == null || !(parameter is string strParameter))
        {
            return false;
        }

        var dateTimes = values
            .Where(v => v != null)
            .Select(v =>
                v switch
                {
                    DateTime dt => (DateTime?)dt,
                    DateTimeOffset dto => (DateTime?)dto.LocalDateTime,
                    _ => null
                })
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToArray();

        return ApplyComparison(strParameter, dateTimes);
    }

    private bool ApplyComparison(string compareType, params DateTime[] values)
    {
        var source = values.FirstOrDefault();
        if (source == default)
        {
            return false;
        }

        var result = values.All(v => v.Year == source.Year);
        if (compareType.Equals(ComparisonMode.Year.ToString(), StringComparison.OrdinalIgnoreCase) || !result)
        {
            return result;
        }

        result = values.All(v => v.Month == source.Month);
        if (compareType.Equals(ComparisonMode.Month.ToString(), StringComparison.OrdinalIgnoreCase) || !result)
        {
            return result;
        }

        result = values.All(v => v.Day == source.Day);
        if (compareType.Equals(ComparisonMode.Day.ToString(), StringComparison.OrdinalIgnoreCase) || !result)
        {
            return result;
        }

        result = values.All(v => v.Hour == source.Hour);
        if (compareType.Equals(ComparisonMode.Hour.ToString(), StringComparison.OrdinalIgnoreCase) || !result)
        {
            return result;
        }

        result = values.All(v => v.Minute == source.Minute);
        if (compareType.Equals(ComparisonMode.Minute.ToString(), StringComparison.OrdinalIgnoreCase) || !result)
        {
            return result;
        }

        result = values.All(v => v.Second == source.Second);
        return result;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        if (!(value is bool b) || targetTypes.Any(t => !t.IsAssignableFrom(typeof(bool))))
        {
            // Return null to indicate conversion back is not possible
            return null;
        }

        if (b)
        {
            return targetTypes.Select(t => (object)true).ToArray();
        }
        else
        {
            // Can't convert back from false because of ambiguity
            return null;
        }
    }
}