using CommunityToolkit.Maui;
using System.Collections;
using System.Windows.Input;

namespace Querify.UI.Views.Controls.Trees;

/// <summary>
/// Arbre hierarchique generique (MAUI n'en fournit pas) : chaque element devient un
/// <see cref="TreeViewNode"/> qui heberge ses enfants, sans aplatir l'arbre.
/// Les chemins (<see cref="ChildrenPath"/>, <see cref="IsExpandedPath"/>, <see cref="CanExpandPath"/>)
/// evitent d'imposer une interface aux modeles ; <see cref="ExpandingCommand"/> permet de
/// charger les enfants a la premiere ouverture.
///
/// Exemple :
/// <![CDATA[
///   <v:TreeView ItemsSource="{Binding Roots}"
///               SelectedItem="{Binding SelectedNode}"
///               ExpandingCommand="{Binding ExpandNodeCommand}">
///       <v:TreeView.ItemTemplate>
///           <DataTemplate x:DataType="m:Node">
///               <Label Text="{Binding Title}" />
///           </DataTemplate>
///       </v:TreeView.ItemTemplate>
///   </v:TreeView>
/// ]]>
/// </summary>
public partial class TreeView : ContentView
{
    private TreeViewNode? _selectedNode;

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

    public TreeView()
    {
        InitializeComponent();
        RebuildNodes();
    }

    private void OnTreeScrollHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        AttachKeyboard();
#endif
    }

#if WINDOWS
    private global::Microsoft.UI.Xaml.Controls.ScrollViewer? _nativeScroll;

    // Les noeuds ne sont pas focalisables : le ScrollViewer natif sert de cible au clavier.
    private void AttachKeyboard()
    {
        if (_nativeScroll is not null)
            _nativeScroll.KeyDown -= OnNativeKeyDown;

        _nativeScroll = TreeScroll.Handler?.PlatformView as global::Microsoft.UI.Xaml.Controls.ScrollViewer;
        if (_nativeScroll is null)
            return;

        _nativeScroll.IsTabStop = true;
        _nativeScroll.UseSystemFocusVisuals = false;
        _nativeScroll.KeyDown += OnNativeKeyDown;
    }

    // Un clic sur un noeud donne le focus clavier a l'arbre (FocusState.Pointer : pas de cadre de focus).
    private void FocusTree()
        => _nativeScroll?.Focus(global::Microsoft.UI.Xaml.FocusState.Pointer);

    private void OnNativeKeyDown(object sender, global::Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key != global::Windows.System.VirtualKey.Delete)
            return;

        // CanExecute decide si l'element supporte la suppression (le VM filtre par type de noeud).
        if (SelectedItem is { } item && DeleteCommand is { } command && command.CanExecute(item))
        {
            command.Execute(item);
            e.Handled = true;
        }
    }
#endif

    // ItemTemplate et Indent sont lus a la creation des noeuds : en XAML, ItemsSource peut
    // etre resolu avant que l'ItemTemplate (element de propriete) ne soit affecte.
    private void RebuildNodes()
    {
        _selectedNode = null;
        BindableLayout.SetItemTemplate(NodesHost, new DataTemplate(() => new TreeViewNode(this, level: 0)));
    }

    /// <summary>Clic sur un en-tete.</summary>
    internal void Select(TreeViewNode node)
    {
        SelectedItem = node.BindingContext;

#if WINDOWS
        FocusTree();
#endif

        // Element present plusieurs fois ou SelectedItem refuse par le binding : on force l'etat visuel.
        if (_selectedNode != node)
            SetSelectedNode(node);
    }

    /// <summary>Un noeud recycle ou retire ne doit plus etre tenu pour selectionne.</summary>
    internal void Release(TreeViewNode node)
    {
        if (_selectedNode == node)
            _selectedNode = null;
    }

    /// <summary>Un noeud cree apres coup (rechargement) reprend la selection si c'est son element.</summary>
    internal void Attach(TreeViewNode node)
    {
        if (SelectedItem is not null && Equals(node.BindingContext, SelectedItem) && _selectedNode != node)
            SetSelectedNode(node);
    }

    // propertyChanged indispensable : la selection peut venir du VM (rechargement, suppression).
    private void OnSelectedItemChanged(object? item)
    {
        if (_selectedNode?.BindingContext is { } current && Equals(current, item))
            return;

        SetSelectedNode(item is null ? null : FindNode(NodesHost, item));
    }

    private void SetSelectedNode(TreeViewNode? node)
    {
        _selectedNode?.SetSelected(false);
        _selectedNode = node;
        _selectedNode?.SetSelected(true);
    }

    private static TreeViewNode? FindNode(Layout host, object item)
    {
        foreach (var child in host.Children)
        {
            if (child is not TreeViewNode node)
                continue;

            if (Equals(node.BindingContext, item))
                return node;

            if (FindNode(node.ChildrenHost, item) is { } found)
                return found;
        }

        return null;
    }
}
