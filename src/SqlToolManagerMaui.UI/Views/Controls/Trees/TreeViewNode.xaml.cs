using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.UI.Views.Controls.Trees;

/// <summary>
/// Noeud d'un <see cref="TreeView"/> : en-tete (chevron + ItemTemplate) puis, sous lui,
/// ses propres enfants (meme gabarit, un niveau plus loin). Interne au TreeView.
/// </summary>
public sealed partial class TreeViewNode : ContentView
{
    /// <summary>Arbre proprietaire : source de l'apparence et des chemins de binding.</summary>
    public TreeView Owner { get; }

    /// <summary>Etat d'ouverture, lie en TwoWay au chemin <see cref="TreeView.IsExpandedPath"/> de l'element.</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnIsExpandedChanged))]
    public partial bool IsExpanded { get; set; }

    [BindableProperty]
    public partial bool CanExpand { get; set; }

    [BindableProperty]
    public partial bool IsSelected { get; private set; }

    private static void OnIsExpandedChanged(BindableObject bindable, object oldValue, object newValue)
        => ((TreeViewNode)bindable).OnExpanded((bool)newValue);

    internal TreeViewNode(TreeView owner, int level)
    {
        // Avant InitializeComponent : les bindings du XAML passent par Owner.
        Owner = owner;
        InitializeComponent();

        HeaderRow.Padding = new Thickness(4 + level * owner.Indent, 0, 4, 0);

        var content = owner.ItemTemplate?.CreateContent() as View ?? new Label();
        content.VerticalOptions = LayoutOptions.Center;
        HeaderRow.Add(content, 1);

        BindableLayout.SetItemTemplate(ChildrenHost, new DataTemplate(() => new TreeViewNode(owner, level + 1)));
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        // Chemins lus a la creation : ils sont declares une fois sur le TreeView.
        SetBinding(IsExpandedProperty, new Binding(Owner.IsExpandedPath, BindingMode.TwoWay));
        SetBinding(CanExpandProperty, new Binding(Owner.CanExpandPath));
        ChildrenHost.SetBinding(BindableLayout.ItemsSourceProperty, new Binding(Owner.ChildrenPath));

        Owner.Attach(this);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is null)
            Owner.Release(this);
    }

    internal void SetSelected(bool isSelected) => IsSelected = isSelected;

    private void OnHeaderTapped(object? sender, TappedEventArgs e) => Owner.Select(this);

    private void OnToggleTapped(object? sender, TappedEventArgs e) => Toggle();

    private void OnHeaderPointerEntered(object? sender, PointerEventArgs e) => HoverBackground.Opacity = 1;

    private void OnHeaderPointerExited(object? sender, PointerEventArgs e) => HoverBackground.Opacity = 0;

    private void Toggle()
    {
        if (!CanExpand)
            return;

        // Shift gauche + clic sur un noeud ouvert : referme aussi toute sa descendance.
        // Pas d'equivalent a l'ouverture : tout deplier chargerait trop d'elements.
        if (IsExpanded && IsLeftShiftPressed())
        {
            CollapseAll();
            return;
        }

        IsExpanded = !IsExpanded;
    }

    private void CollapseAll()
    {
        foreach (var child in ChildrenHost.Children.OfType<TreeViewNode>())
            child.CollapseAll();

        IsExpanded = false;
    }

    private static bool IsLeftShiftPressed()
    {
#if WINDOWS
        var state = global::Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(global::Windows.System.VirtualKey.LeftShift);
        return state.HasFlag(global::Windows.UI.Core.CoreVirtualKeyStates.Down);
#else
        return false;
#endif
    }

    private void OnExpanded(bool isExpanded)
    {
        if (isExpanded && Owner.ExpandingCommand is { } command && command.CanExecute(BindingContext))
            command.Execute(BindingContext);
    }
}
