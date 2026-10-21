using CommunityToolkit.Maui;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace SqlToolManagerMaui.UI.Views.Controls.DataGrids;

/// <summary>
/// Colonne d'un <see cref="DataGrid"/>. La valeur est lue par <see cref="ValueGetter"/> (colonnes construites en code,
/// sans reflexion) ou, a defaut, par <see cref="Path"/> (colonnes declarees en XAML ou generees).
/// </summary>
public partial class DataGridColumn : BindableObject
{
    private readonly Dictionary<Type, PropertyInfo[]?> _pathCache = [];

    [BindableProperty(PropertyChangedMethodName = nameof(OnHeaderChanged))]
    public partial string Header { get; set; } = string.Empty;

    [BindableProperty]
    public partial string? ToolTip { get; set; }

    /// <summary>Chemin de la valeur sur l'element ("Nom", "Client.Ville") ; vide = l'element lui-meme.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnPathChanged))]
    public partial string? Path { get; set; }

    /// <summary>Format des valeurs formatables ("N2", "dd/MM/yyyy"...), en culture courante.</summary>
    [BindableProperty]
    public partial string? StringFormat { get; set; }

    /// <summary>Largeur fixe ; 0 = estimee d'apres l'en-tete et les premieres lignes.</summary>
    [BindableProperty]
    public partial double Width { get; set; }

    [BindableProperty]
    public partial bool CanSort { get; set; } = true;

    [BindableProperty]
    public partial bool CanFilter { get; set; } = true;

    /// <summary>Filtre saisi via le bouton de l'en-tete (syntaxe de <c>TextFilter</c> : texte, !texte, =, !=, &gt;, &lt;, NULL...).</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial string FilterText { get; set; } = string.Empty;

    /// <summary>Tri applique par la grille ; une seule colonne triee a la fois.</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial DataGridSortDirection SortDirection { get; set; }

    /// <summary>Identifiant de la colonne dans <c>DataGrid.SortMember</c> et <c>DataGrid.Filters</c> : <see cref="Path"/>, sinon <see cref="Header"/>.</summary>
    [BindableProperty]
    public partial string Key { get; private set; } = string.Empty;

    /// <summary>Lecture de la valeur, prioritaire sur <see cref="Path"/>.</summary>
    [BindableProperty]
    public partial Func<object, object?>? ValueGetter { get; set; }

    private static void OnHeaderChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DataGridColumn)bindable).UpdateKey();

    private static void OnPathChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var column = (DataGridColumn)bindable;
        column._pathCache.Clear();
        column.UpdateKey();
    }

    private void UpdateKey()
        => Key = string.IsNullOrWhiteSpace(Path) ? Header : Path;

    public object? ReadValue(object item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (ValueGetter is { } getter)
            return getter(item);

        if (string.IsNullOrWhiteSpace(Path))
            return item;

        object? current = item;
        foreach (var property in ResolvePath(item.GetType()) ?? [])
        {
            if (current is null)
                return null;

            current = property.GetValue(current);
        }

        return current;
    }

    /// <summary>Texte affiche ; null pour une valeur absente (la grille affiche alors son texte NULL).</summary>
    public string? FormatValue(object? value) => value switch
    {
        null or DBNull => null,
        string text => text,
        IFormattable formattable => formattable.ToString(string.IsNullOrEmpty(StringFormat) ? null : StringFormat, CultureInfo.CurrentCulture),
        _ => value.ToString(),
    };

    // Chaine de proprietes resolue une fois par type d'element ; null si un segment n'existe pas.
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Application non rognee ; ValueGetter evite la reflexion si besoin.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Application non rognee ; ValueGetter evite la reflexion si besoin.")]
    private PropertyInfo[]? ResolvePath(Type itemType)
    {
        if (_pathCache.TryGetValue(itemType, out var cached))
            return cached;

        var segments = Path!.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var chain = new PropertyInfo[segments.Length];
        var currentType = itemType;

        for (var i = 0; i < segments.Length; i++)
        {
            var property = currentType.GetProperty(segments[i], BindingFlags.Public | BindingFlags.Instance);
            if (property is null || property.GetIndexParameters().Length > 0)
            {
                chain = null;
                break;
            }

            chain[i] = property;
            currentType = property.PropertyType;
        }

        _pathCache[itemType] = chain;
        return chain;
    }
}
