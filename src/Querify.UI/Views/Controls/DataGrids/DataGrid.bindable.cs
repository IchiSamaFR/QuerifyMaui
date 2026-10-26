using CommunityToolkit.Maui;
using System.Collections;
using System.Windows.Input;

namespace Querify.UI.Views.Controls.DataGrids;

public partial class DataGrid
{
    /// <summary>Elements affiches ; une collection observable est suivie (ajouts, suppressions).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnItemsSourcePropertyChanged))]
    public partial IEnumerable? ItemsSource { get; set; }

    /// <summary>Colonnes declarees (enfants XAML) ou fournies par binding ; affichees avant les colonnes generees.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnColumnsPropertyChanged), DefaultValueCreatorMethodName = nameof(CreateDefaultColumns))]
    public partial IList<DataGridColumn>? Columns { get; set; }

    /// <summary>Ajoute une colonne par propriete publique des elements (comme le DataGrid WPF).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnLayoutPropertyChanged))]
    public partial bool AutoGenerateColumns { get; set; } = true;

    /// <summary>Texte d'une cellule sans valeur.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnLayoutPropertyChanged))]
    public partial string NullValueText { get; set; } = "NULL";

    /// <summary>Tri au clic sur un en-tete (cycle croissant, decroissant, aucun).</summary>
    [BindableProperty]
    public partial bool CanUserSort { get; set; } = true;

    /// <summary>Affiche un bouton de filtre a droite du titre de chaque colonne filtrable.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnLayoutPropertyChanged))]
    public partial bool CanUserFilter { get; set; } = true;

    /// <summary>None ou Single ; avec None, <see cref="ItemTappedCommand"/> reste actif.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectionBehaviorPropertyChanged))]
    public partial SelectionMode SelectionMode { get; set; } = SelectionMode.None;

    /// <summary>Element de la source selectionne (et non la ligne affichee).</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnSelectedItemPropertyChanged))]
    public partial object? SelectedItem { get; set; }

    /// <summary>Clic sur une ligne (parametre : l'element de la source).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectionBehaviorPropertyChanged))]
    public partial ICommand? ItemTappedCommand { get; set; }

    /// <summary>Nombre d'elements de la source.</summary>
    [BindableProperty]
    public partial int ItemsCount { get; private set; }

    /// <summary>Nombre de lignes affichees apres filtre.</summary>
    [BindableProperty]
    public partial int DisplayedItemsCount { get; private set; }

    /// <summary>Vrai si au moins un filtre de colonne est actif.</summary>
    [BindableProperty]
    public partial bool IsFiltered { get; private set; }

    [BindableProperty]
    public partial bool HasNoMatchingItems { get; private set; }

    [BindableProperty]
    public partial bool HasColumns { get; private set; }

    /// <summary>Cle (<see cref="DataGridColumn.Key"/>) de la colonne triee ; null = aucun tri.</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnSortStatePropertyChanged))]
    public partial string? SortMember { get; set; }

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnSortStatePropertyChanged))]
    public partial DataGridSortDirection SortDirection { get; set; }

    /// <summary>
    /// Filtres saisis, par cle de colonne (<see cref="DataGridColumn.Key"/>). Remplace en entier a chaque changement ;
    /// conserve quand les colonnes changent, et reapplique aux colonnes de meme cle.
    /// </summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnFiltersPropertyChanged))]
    public partial IReadOnlyDictionary<string, string>? Filters { get; set; }

    /// <summary>Tri d'une colonne (parametre : la colonne), utilise par les en-tetes.</summary>
    [BindableProperty(DefaultValueCreatorMethodName = nameof(CreateSortCommand))]
    public partial ICommand SortCommand { get; private set; }

    /// <summary>Ouvre (ou referme) la saisie de filtre d'une colonne (parametre : la colonne), utilise par les en-tetes.</summary>
    [BindableProperty(DefaultValueCreatorMethodName = nameof(CreateFilterCommand))]
    public partial ICommand FilterCommand { get; private set; }

    [BindableProperty(DefaultValueCreatorMethodName = nameof(CreateCloseFilterCommand))]
    public partial ICommand CloseFilterCommand { get; private set; }

    /// <summary>Vide les filtres de toutes les colonnes.</summary>
    [BindableProperty(DefaultValueCreatorMethodName = nameof(CreateClearFiltersCommand))]
    public partial ICommand ClearFiltersCommand { get; private set; }

    /// <summary>Colonne dont la saisie de filtre est ouverte.</summary>
    [BindableProperty]
    public partial DataGridColumn? EditedFilterColumn { get; private set; }

    [BindableProperty]
    public partial bool IsFilterEditorOpen { get; private set; }
}
