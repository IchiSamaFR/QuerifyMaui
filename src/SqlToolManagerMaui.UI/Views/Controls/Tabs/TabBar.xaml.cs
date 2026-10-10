using CommunityToolkit.Maui;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace SqlToolManagerMaui.UI.Views.Controls.Tabs;

/// <summary>
/// Controle a onglets facon WPF / Windows 11 : barre d'onglets en haut, contenu de l'onglet selectionne en dessous.
/// Onglets declares en enfants (<see cref="Items"/>) ou generes depuis <see cref="ItemsSource"/> via <see cref="ItemTemplate"/>.
/// Contenu affiche : <see cref="TabBarItem.Content"/>, sinon <see cref="ContentTemplate"/>, sinon <see cref="DefaultContent"/>.
/// </summary>
[ContentProperty(nameof(Items))]
public partial class TabBar : ContentView
{
    [BindableProperty(PropertyChangedMethodName = nameof(OnItemsSourceChanged))]
    public partial IEnumerable? ItemsSource { get; set; }

    /// <summary>Doit produire un <see cref="TabBarItem"/> ; son BindingContext est l'element de la source.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnTabsDefinitionChanged))]
    public partial DataTemplate? ItemTemplate { get; set; }

    /// <summary>Contenu genere pour l'element selectionne (BindingContext = <see cref="SelectedItem"/>).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnContentDefinitionChanged))]
    public partial DataTemplate? ContentTemplate { get; set; }

    /// <summary>Contenu partage, affiche quand l'onglet selectionne n'en fournit pas.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnContentDefinitionChanged))]
    public partial View? DefaultContent { get; set; }

    /// <summary>Element de <see cref="ItemsSource"/>, ou le <see cref="TabBarItem"/> en mode <see cref="Items"/>.</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnSelectedItemChanged))]
    public partial object? SelectedItem { get; set; }

    /// <summary>Recoit l'element de l'onglet a fermer en parametre.</summary>
    [BindableProperty]
    public partial ICommand? CloseCommand { get; set; }

    /// <summary>Contenu affiche a droite des onglets (barre d'outils, bouton "+").</summary>
    [BindableProperty]
    public partial View? TrailingContent { get; set; }

    [BindableProperty]
    public partial View? SelectedContent { get; private set; }

    public IList<TabBarItem> Items { get; } = new ObservableCollection<TabBarItem>();

    private readonly ObservableCollection<TabBarItem> _tabs = [];
    private INotifyCollectionChanged? _observedSource;
    private object? _templatedContentItem;
    private View? _templatedContent;

    public TabBar()
    {
        InitializeComponent();

        ((ObservableCollection<TabBarItem>)Items).CollectionChanged += OnItemsChanged;
        BindableLayout.SetItemTemplate(TabsHost, new DataTemplate(() => new TabBarHeader()));
        BindableLayout.SetItemsSource(TabsHost, _tabs);
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        foreach (var tab in Items)
            SetInheritedBindingContext(tab, BindingContext);
    }

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var bar = (TabBar)bindable;

        if (bar._observedSource is not null)
            bar._observedSource.CollectionChanged -= bar.OnSourceCollectionChanged;

        bar._observedSource = newValue as INotifyCollectionChanged;
        if (bar._observedSource is not null)
            bar._observedSource.CollectionChanged += bar.OnSourceCollectionChanged;

        bar.RebuildTabs();
    }

    private static void OnTabsDefinitionChanged(BindableObject bindable, object oldValue, object newValue)
        => ((TabBar)bindable).RebuildTabs(reuseTabs: false);

    private static void OnContentDefinitionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var bar = (TabBar)bindable;
        bar._templatedContentItem = null;
        bar._templatedContent = null;
        bar.UpdateSelection();
    }

    private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
        => ((TabBar)bindable).UpdateSelection(scroll: true);

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (ItemsSource is null)
            RebuildTabs();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildTabs();

    private void RebuildTabs(bool reuseTabs = true)
    {
        // Les onglets deja generes sont reutilises : leur contenu (et son etat) survit aux ajouts/suppressions.
        var previous = reuseTabs ? _tabs.ToList() : [];
        _tabs.Clear();

        if (ItemsSource is null)
        {
            foreach (var tab in Items)
            {
                SetInheritedBindingContext(tab, BindingContext);
                _tabs.Add(tab);
            }

            if (SelectedItem is null && _tabs.Count > 0)
            {
                SelectedItem = _tabs[0];
                return;
            }
        }
        else
        {
            foreach (var data in ItemsSource)
                _tabs.Add(previous.FirstOrDefault(t => t.DataItem is not null && Equals(t.DataItem, data)) ?? CreateTab(data));
        }

        UpdateSelection();
    }

    private TabBarItem CreateTab(object data)
    {
        var template = ItemTemplate is DataTemplateSelector selector ? selector.SelectTemplate(data, this) : ItemTemplate;

        var tab = template is null
            ? new TabBarItem { Header = data.ToString() }
            : template.CreateContent() as TabBarItem
              ?? throw new InvalidOperationException($"L'{nameof(ItemTemplate)} d'une {nameof(TabBar)} doit produire un {nameof(TabBarItem)}.");

        tab.DataItem = data;
        tab.BindingContext = data;
        return tab;
    }

    private void UpdateSelection(bool scroll = false)
    {
        var selected = _tabs.FirstOrDefault(t => Equals(t.Value, SelectedItem));

        foreach (var tab in _tabs)
            tab.SetSelected(ReferenceEquals(tab, selected));

        SelectedContent = selected?.Content ?? GetTemplatedContent() ?? DefaultContent;

        if (scroll && selected is not null)
            ScrollToHeader(selected);
    }

    private View? GetTemplatedContent()
    {
        if (ContentTemplate is null || SelectedItem is null)
            return null;

        if (!Equals(_templatedContentItem, SelectedItem))
        {
            var template = ContentTemplate is DataTemplateSelector selector ? selector.SelectTemplate(SelectedItem, this) : ContentTemplate;
            _templatedContent = template?.CreateContent() as View;
            if (_templatedContent is not null)
                _templatedContent.BindingContext = SelectedItem;
            _templatedContentItem = SelectedItem;
        }

        return _templatedContent;
    }

    private void OnHeaderAdded(object? sender, ElementEventArgs e)
    {
        if (e.Element is not TabBarHeader header)
            return;

        header.Selected += OnHeaderSelected;
        header.CloseRequested += OnHeaderCloseRequested;
    }

    private void OnHeaderRemoved(object? sender, ElementEventArgs e)
    {
        if (e.Element is not TabBarHeader header)
            return;

        header.Selected -= OnHeaderSelected;
        header.CloseRequested -= OnHeaderCloseRequested;
    }

    private void OnHeaderSelected(object? sender, EventArgs e)
    {
        if (sender is TabBarHeader { BindingContext: TabBarItem tab })
            SelectedItem = tab.Value;
    }

    private void OnHeaderCloseRequested(object? sender, EventArgs e)
    {
        if (sender is not TabBarHeader { BindingContext: TabBarItem tab })
            return;

        var parameter = tab.Value;
        if (CloseCommand?.CanExecute(parameter) == true)
            CloseCommand.Execute(parameter);
    }

    private void ScrollToHeader(TabBarItem tab)
    {
        // L'en-tete n'existe pas encore tant que le BindableLayout n'a pas reagi a l'ajout.
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(80), async () =>
        {
            var header = TabsHost.Children
                .OfType<TabBarHeader>()
                .FirstOrDefault(h => ReferenceEquals(h.BindingContext, tab));

            if (header is not null)
                await TabScroll.ScrollToAsync(header, ScrollToPosition.MakeVisible, animated: true);
        });
    }
}
