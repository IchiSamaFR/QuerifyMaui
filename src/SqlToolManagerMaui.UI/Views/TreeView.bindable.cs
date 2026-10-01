using System.Collections;
using System.Windows.Input;

namespace SqlToolManagerMaui.UI.Views;

public partial class TreeView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(TreeView), null);

    /// <summary>Elements racines.</summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(TreeView), null,
        propertyChanged: (b, _, _) => ((TreeView)b).RebuildNodes());

    /// <summary>Contenu de l'en-tete d'un noeud (a droite du chevron), lie a l'element. Pas de DataTemplateSelector.</summary>
    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly BindableProperty ChildrenPathProperty = BindableProperty.Create(
        nameof(ChildrenPath), typeof(string), typeof(TreeView), "Children");

    /// <summary>Chemin de la collection d'enfants sur chaque element (ObservableCollection conseillee).</summary>
    public string ChildrenPath
    {
        get => (string)GetValue(ChildrenPathProperty);
        set => SetValue(ChildrenPathProperty, value);
    }

    public static readonly BindableProperty IsExpandedPathProperty = BindableProperty.Create(
        nameof(IsExpandedPath), typeof(string), typeof(TreeView), "IsExpanded");

    /// <summary>Chemin du booleen d'ouverture sur chaque element, lie en TwoWay.</summary>
    public string IsExpandedPath
    {
        get => (string)GetValue(IsExpandedPathProperty);
        set => SetValue(IsExpandedPathProperty, value);
    }

    public static readonly BindableProperty CanExpandPathProperty = BindableProperty.Create(
        nameof(CanExpandPath), typeof(string), typeof(TreeView), "CanExpand");

    /// <summary>Chemin du booleen "peut etre deplie" : permet le chevron avant tout chargement.</summary>
    public string CanExpandPath
    {
        get => (string)GetValue(CanExpandPathProperty);
        set => SetValue(CanExpandPathProperty, value);
    }

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem), typeof(object), typeof(TreeView), null, BindingMode.TwoWay,
        propertyChanged: (b, _, n) => ((TreeView)b).OnSelectedItemChanged(n));

    /// <summary>Element selectionne (clic sur un en-tete).</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly BindableProperty ExpandingCommandProperty = BindableProperty.Create(
        nameof(ExpandingCommand), typeof(ICommand), typeof(TreeView), null);

    /// <summary>Execute avec l'element a chaque ouverture : point d'accroche du chargement paresseux.</summary>
    public ICommand? ExpandingCommand
    {
        get => (ICommand?)GetValue(ExpandingCommandProperty);
        set => SetValue(ExpandingCommandProperty, value);
    }

    public static readonly BindableProperty IndentProperty = BindableProperty.Create(
        nameof(Indent), typeof(double), typeof(TreeView), 16d,
        propertyChanged: (b, _, _) => ((TreeView)b).RebuildNodes());

    /// <summary>Decalage horizontal d'un niveau.</summary>
    public double Indent
    {
        get => (double)GetValue(IndentProperty);
        set => SetValue(IndentProperty, value);
    }

    public static readonly BindableProperty ItemHeightProperty = BindableProperty.Create(
        nameof(ItemHeight), typeof(double), typeof(TreeView), 28d);

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public static readonly BindableProperty ItemCornerRadiusProperty = BindableProperty.Create(
        nameof(ItemCornerRadius), typeof(double), typeof(TreeView), 4d);

    public double ItemCornerRadius
    {
        get => (double)GetValue(ItemCornerRadiusProperty);
        set => SetValue(ItemCornerRadiusProperty, value);
    }

    public static readonly BindableProperty ItemHoverColorProperty = BindableProperty.Create(
        nameof(ItemHoverColor), typeof(Color), typeof(TreeView), Colors.White.WithAlpha(0.04f));

    public Color ItemHoverColor
    {
        get => (Color)GetValue(ItemHoverColorProperty);
        set => SetValue(ItemHoverColorProperty, value);
    }

    public static readonly BindableProperty ItemSelectedColorProperty = BindableProperty.Create(
        nameof(ItemSelectedColor), typeof(Color), typeof(TreeView), Colors.White.WithAlpha(0.06f));

    public Color ItemSelectedColor
    {
        get => (Color)GetValue(ItemSelectedColorProperty);
        set => SetValue(ItemSelectedColorProperty, value);
    }

    public static readonly BindableProperty SelectionIndicatorColorProperty = BindableProperty.Create(
        nameof(SelectionIndicatorColor), typeof(Color), typeof(TreeView), Colors.DodgerBlue);

    /// <summary>Pastille verticale a gauche de l'element selectionne (Transparent pour la masquer).</summary>
    public Color SelectionIndicatorColor
    {
        get => (Color)GetValue(SelectionIndicatorColorProperty);
        set => SetValue(SelectionIndicatorColorProperty, value);
    }

    public static readonly BindableProperty ChevronColorProperty = BindableProperty.Create(
        nameof(ChevronColor), typeof(Color), typeof(TreeView), Colors.Gray);

    public Color ChevronColor
    {
        get => (Color)GetValue(ChevronColorProperty);
        set => SetValue(ChevronColorProperty, value);
    }
}
