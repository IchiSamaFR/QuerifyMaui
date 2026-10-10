using CommunityToolkit.Maui;
using System.Collections;
using System.Windows.Input;

namespace SqlToolManagerMaui.UI.Views.Controls.Trees;

public partial class TreeView
{
    /// <summary>Elements racines.</summary>
    [BindableProperty]
    public partial IEnumerable? ItemsSource { get; set; }

    /// <summary>Contenu de l'en-tete d'un noeud (a droite du chevron), lie a l'element. Pas de DataTemplateSelector.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnNodesLayoutChanged))]
    public partial DataTemplate? ItemTemplate { get; set; }

    /// <summary>Chemin de la collection d'enfants sur chaque element (ObservableCollection conseillee).</summary>
    [BindableProperty]
    public partial string ChildrenPath { get; set; } = "Children";

    /// <summary>Chemin du booleen d'ouverture sur chaque element, lie en TwoWay.</summary>
    [BindableProperty]
    public partial string IsExpandedPath { get; set; } = "IsExpanded";

    /// <summary>Chemin du booleen "peut etre deplie" : permet le chevron avant tout chargement.</summary>
    [BindableProperty]
    public partial string CanExpandPath { get; set; } = "CanExpand";

    /// <summary>Element selectionne (clic sur un en-tete).</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnSelectedItemPropertyChanged))]
    public partial object? SelectedItem { get; set; }

    /// <summary>Execute avec l'element a chaque ouverture : point d'accroche du chargement paresseux.</summary>
    [BindableProperty]
    public partial ICommand? ExpandingCommand { get; set; }

    /// <summary>
    /// Execute avec l'element selectionne quand l'utilisateur appuie sur Suppr (Windows).
    /// <see cref="ICommand.CanExecute"/> sert de filtre : s'il refuse l'element, la touche est ignoree.
    /// </summary>
    [BindableProperty]
    public partial ICommand? DeleteCommand { get; set; }

    /// <summary>Decalage horizontal d'un niveau.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnNodesLayoutChanged))]
    public partial double Indent { get; set; } = 16d;

    [BindableProperty]
    public partial double ItemHeight { get; set; } = 28d;

    [BindableProperty]
    public partial double ItemCornerRadius { get; set; } = 4d;

    [BindableProperty]
    public partial Color ItemHoverColor { get; set; } = Colors.White.WithAlpha(0.04f);

    [BindableProperty]
    public partial Color ItemSelectedColor { get; set; } = Colors.White.WithAlpha(0.06f);

    /// <summary>Pastille verticale a gauche de l'element selectionne (Transparent pour la masquer).</summary>
    [BindableProperty]
    public partial Color SelectionIndicatorColor { get; set; } = Colors.DodgerBlue;

    [BindableProperty]
    public partial Color ChevronColor { get; set; } = Colors.Gray;

    private static void OnNodesLayoutChanged(BindableObject bindable, object oldValue, object newValue)
        => ((TreeView)bindable).RebuildNodes();

    private static void OnSelectedItemPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((TreeView)bindable).OnSelectedItemChanged(newValue);
}
