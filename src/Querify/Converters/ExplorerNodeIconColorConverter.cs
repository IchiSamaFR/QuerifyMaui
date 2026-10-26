using Querify.Core.Enums.Databases;
using System.Globalization;

namespace Querify.Converters;

/// <summary>Couleur d'icone propre a chaque type d'objet : Kind, IsPrimaryKey, IsForeignKey.</summary>
public sealed class ExplorerNodeIconColorConverter : IMultiValueConverter
{
    public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [ExplorerNodeKind kind, bool isPrimaryKey, bool isForeignKey])
            return null;

        return kind switch
        {
            ExplorerNodeKind.Connection => Color.FromArgb("#4FC3F7"),
            ExplorerNodeKind.Database => Color.FromArgb("#FF8A65"),
            ExplorerNodeKind.Table => Color.FromArgb("#81C784"),
            ExplorerNodeKind.View => Color.FromArgb("#BA68C8"),
            ExplorerNodeKind.StoredProcedure => Color.FromArgb("#F06292"),
            ExplorerNodeKind.Column when isPrimaryKey => Color.FromArgb("#FFD54F"),
            ExplorerNodeKind.Column when isForeignKey => Color.FromArgb("#64B5F6"),
            ExplorerNodeKind.Column => Color.FromArgb("#B0BEC5"),
            ExplorerNodeKind.Message => Color.FromArgb("#87FFFFFF"),
            _ => Color.FromArgb("#E8C468"),
        };
    }

    public object?[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
