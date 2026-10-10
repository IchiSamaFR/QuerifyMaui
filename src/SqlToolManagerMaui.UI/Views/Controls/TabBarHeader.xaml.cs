using CommunityToolkit.Maui;
using System.ComponentModel;

namespace SqlToolManagerMaui.UI.Views.Controls;

/// <summary>En-tete d'un <see cref="TabBarItem"/> (son BindingContext), genere par la <see cref="TabBar"/>.</summary>
public partial class TabBarHeader : ContentView
{
    [BindableProperty]
    public partial bool IsPointerOver { get; private set; }

    [BindableProperty]
    public partial bool ShowDirtyDot { get; private set; }

    [BindableProperty]
    public partial bool ShowCloseButton { get; private set; }

    public event EventHandler? Selected;

    public event EventHandler? CloseRequested;

    private TabBarItem? _tab;

    public TabBarHeader()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_tab is not null)
            _tab.PropertyChanged -= OnTabPropertyChanged;

        _tab = BindingContext as TabBarItem;
        if (_tab is not null)
            _tab.PropertyChanged += OnTabPropertyChanged;

        Refresh();
    }

    private void OnTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TabBarItem.IsDirty) or nameof(TabBarItem.IsSelected) or nameof(TabBarItem.CanClose))
            Refresh();
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        IsPointerOver = true;
        Refresh();
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        IsPointerOver = false;
        Refresh();
    }

    private void OnTapped(object? sender, TappedEventArgs e) => Selected?.Invoke(this, EventArgs.Empty);

    private void OnCloseClicked(object? sender, EventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

    // Pastille "modifie" hors survol ; bouton fermer au survol et sur l'onglet actif.
    private void Refresh()
    {
        ShowDirtyDot = _tab?.IsDirty == true && !IsPointerOver;
        ShowCloseButton = _tab?.CanClose == true && !ShowDirtyDot && (IsPointerOver || _tab.IsSelected);
    }
}
