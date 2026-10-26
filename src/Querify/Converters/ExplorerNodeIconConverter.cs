using MauiIcons.Fluent.Filled;
using Querify.Core.Enums.Databases;
using System.Globalization;

namespace Querify.Converters;

/// <summary>Icone d'un noeud de l'explorateur : Kind, IsExpanded, IsPrimaryKey, IsForeignKey.</summary>
public sealed class ExplorerNodeIconConverter : IMultiValueConverter
{
    public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [ExplorerNodeKind kind, bool isExpanded, bool isPrimaryKey, bool isForeignKey])
            return null;

        return kind switch
        {
            ExplorerNodeKind.Connection => FluentFilledIcons.Server16Filled,
            ExplorerNodeKind.Database => FluentFilledIcons.Database20Filled,
            ExplorerNodeKind.Table => FluentFilledIcons.Table20Filled,
            ExplorerNodeKind.View => FluentFilledIcons.Eye20Filled,
            ExplorerNodeKind.StoredProcedure => FluentFilledIcons.Code20Filled,
            ExplorerNodeKind.Column when isPrimaryKey => FluentFilledIcons.Key20Filled,
            ExplorerNodeKind.Column when isForeignKey => FluentFilledIcons.Link20Filled,
            ExplorerNodeKind.Column => FluentFilledIcons.Column20Filled,
            ExplorerNodeKind.Message => FluentFilledIcons.Info16Filled,
            _ => isExpanded ? FluentFilledIcons.FolderOpen20Filled : FluentFilledIcons.Folder20Filled,
        };
    }

    public object?[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
