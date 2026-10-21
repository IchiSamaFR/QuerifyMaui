using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SqlToolManagerMaui.UI.Views.Controls.DataGrids;

/// <summary>
/// Generation des colonnes depuis les proprietes publiques des elements, facon WPF.
/// Respecte [Browsable(false)], [Display(Name, Description, Order, AutoGenerateField)] et [DisplayName].
/// </summary>
internal static class DataGridColumnFactory
{
    private const string SimpleValueHeader = "Valeur";

    /// <summary>Type des elements : argument de IEnumerable&lt;T&gt; s'il est plus precis qu'object, sinon type du premier element.</summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Application non rognee ; colonnes explicites si besoin.")]
    public static Type? ResolveItemType(IEnumerable? source)
    {
        if (source is null)
            return null;

        var sourceType = source.GetType();
        var declared = sourceType.GetInterfaces()
            .Append(sourceType)
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(t => t.GetGenericArguments()[0])
            .FirstOrDefault(t => t != typeof(object));
        if (declared is not null)
            return declared;

        foreach (var item in source)
        {
            if (item is not null)
                return item.GetType();
        }

        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Application non rognee ; colonnes explicites si besoin.")]
    public static IReadOnlyList<DataGridColumn> Create(Type itemType)
    {
        ArgumentNullException.ThrowIfNull(itemType);

        // Liste de valeurs simples (textes, nombres...) : une seule colonne, l'element lui-meme.
        if (IsSimpleType(itemType))
            return [new DataGridColumn { Header = SimpleValueHeader }];

        return itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && IsBrowsable(p))
            .Select((property, index) => (Property: property, Display: property.GetCustomAttribute<DisplayAttribute>(), Index: index))
            .OrderBy(p => p.Display?.GetOrder() ?? int.MaxValue)
            .ThenBy(p => p.Index)
            .Select(p => CreateColumn(p.Property, p.Display))
            .ToList();
    }

    private static DataGridColumn CreateColumn(PropertyInfo property, DisplayAttribute? display)
    {
        var header = display?.GetName()
            ?? property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
            ?? property.Name;

        return new DataGridColumn
        {
            Header = header,
            ToolTip = display?.GetDescription() ?? $"{header} ({FriendlyTypeName(property.PropertyType)})",
            Path = property.Name,
            // Lecture directe : evite de resoudre le chemin a chaque cellule.
            ValueGetter = property.GetValue,
        };
    }

    private static bool IsBrowsable(PropertyInfo property)
        => property.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false
            && property.GetCustomAttribute<DisplayAttribute>()?.GetAutoGenerateField() != false;

    private static bool IsSimpleType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(DateOnly)
            || underlying == typeof(TimeOnly)
            || underlying == typeof(TimeSpan)
            || underlying == typeof(Guid);
    }

    private static string FriendlyTypeName(Type type)
        => Nullable.GetUnderlyingType(type) is { } underlying ? $"{underlying.Name}?" : type.Name;
}
