using SqlToolManagerMaui.Models.Editors.Completion;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Liste de suggestions superposee a l'editeur. Elle ne gere ni le clavier ni le focus :
/// c'est <see cref="CompletionController"/> qui la pilote depuis les touches de l'editeur.
/// </summary>
public partial class CompletionPopup : ContentView
{
    public const double RowHeight = 24;
    public const int VisibleRows = 10;
    public const double PopupWidth = 320;

    /// <summary>Padding + bordure du cadre.</summary>
    private const double Chrome = 10;

    private IReadOnlyList<CompletionEntry> _entries = [];
    private int _selectedIndex = -1;

    public CompletionPopup()
    {
        InitializeComponent();
        WidthRequest = PopupWidth;
    }

    public event EventHandler<CompletionItem>? ItemInvoked;

    public CompletionItem? SelectedItem => _selectedIndex >= 0 ? _entries[_selectedIndex].Item : null;

    public double PopupHeight => HeightRequest;

    /// <summary>Remplace la liste en gardant la suggestion courante si elle est encore presente.</summary>
    public void SetItems(IReadOnlyList<CompletionItem> items)
    {
        var previous = SelectedItem;

        _entries = items.Select(item => new CompletionEntry(item)).ToArray();
        _selectedIndex = -1;

        HeightRequest = Math.Min(_entries.Count, VisibleRows) * RowHeight + Chrome;
        BindableLayout.SetItemsSource(Rows, _entries);

        var index = previous is null ? -1 : items.ToList().IndexOf(previous);
        Select(Math.Max(index, 0));
    }

    public void MoveSelection(int delta)
    {
        if (_entries.Count == 0)
            return;

        Select(Math.Clamp(_selectedIndex + delta, 0, _entries.Count - 1));
    }

    private void Select(int index)
    {
        if (index < 0 || index >= _entries.Count)
            return;

        if (_selectedIndex >= 0)
            _entries[_selectedIndex].IsSelected = false;

        _selectedIndex = index;
        _entries[index].IsSelected = true;

        _ = Scroller.ScrollToAsync(0, ScrollOffsetFor(index), false);
    }

    /// <summary>Ne defile que si la ligne sort de la fenetre visible.</summary>
    private double ScrollOffsetFor(int index)
    {
        var top = index * RowHeight;
        var viewport = VisibleRows * RowHeight;

        if (top < Scroller.ScrollY)
            return top;

        return top + RowHeight > Scroller.ScrollY + viewport ? top + RowHeight - viewport : Scroller.ScrollY;
    }

    private void OnRowTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is CompletionEntry entry)
            ItemInvoked?.Invoke(this, entry.Item);
    }
}
