using SqlToolManagerMaui.UI.Views.Controls;

namespace SqlToolManagerMaui.UI.Views.Controls.Trees;

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
    private readonly VerticalStackLayout _rootHost = new();
    private readonly ScrollView _scrollView;
    private TreeViewNode? _selectedNode;

    public TreeView()
    {
        _rootHost.SetBinding(BindableLayout.ItemsSourceProperty, new Binding(nameof(ItemsSource), source: this));
        RebuildNodes();

        _scrollView = new ScrollView
        {
            Orientation = ScrollOrientation.Vertical,
            Content = _rootHost,
        };
        Content = _scrollView;
#if WINDOWS
        _scrollView.HandlerChanged += (_, _) => AttachKeyboard();
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

        _nativeScroll = _scrollView.Handler?.PlatformView as global::Microsoft.UI.Xaml.Controls.ScrollViewer;
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
        BindableLayout.SetItemTemplate(_rootHost, new DataTemplate(() => new TreeViewNode(this, level: 0)));
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

        SetSelectedNode(item is null ? null : FindNode(_rootHost, item));
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
