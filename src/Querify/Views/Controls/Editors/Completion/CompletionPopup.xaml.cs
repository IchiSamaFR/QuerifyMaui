using Quill.Code.Completion;

namespace Querify.Views.Controls.Editors.Completion;

/// <summary>
/// Liste de suggestions superposee a l'editeur. Elle ne gere ni le clavier ni le focus :
/// c'est <see cref="CompletionController"/> qui la pilote depuis les touches de l'editeur.
/// Les <see cref="VisibleRows"/> lignes sont creees une seule fois : filtrer ou defiler ne fait
/// que changer la suggestion qu'elles affichent (virtualisation maison, sans CollectionView).
/// </summary>
public partial class CompletionPopup : ContentView
{
    public const double RowHeight = 24;
    public const int VisibleRows = 10;
    public const double MinPopupWidth = 320;

    /// <summary>Au-dela, le libelle est tronque (le detail reste entier).</summary>
    public const double MaxPopupWidth = 640;

    /// <summary>Padding + bordure du cadre.</summary>
    private const double Chrome = 10;

    /// <summary>Lignes defilees par cran de molette.</summary>
    private const int WheelRows = 3;

    private IReadOnlyList<CompletionItem> _items = [];
    private int _selectedIndex = -1;

    /// <summary>Index de la suggestion affichee sur la premiere ligne.</summary>
    private int _offset;

    /// <summary>Largeur atteinte depuis l'ouverture : la liste ne retrecit pas au defilement.</summary>
    private double _width = MinPopupWidth;

    public CompletionPopup()
    {
        InitializeComponent();

        // Seul le nombre compte : chaque ligne recoit sa proposition de Render.
        BindableLayout.SetItemsSource(Rows, Enumerable.Range(0, VisibleRows).ToArray());

#if WINDOWS
        Surface.HandlerChanged += (_, _) => AttachWheel();
#endif
    }

    public event EventHandler<CompletionItem>? ItemInvoked;

    public CompletionItem? SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

    public double PopupHeight => HeightRequest;

    public double PopupWidth => Math.Min(WidthRequest, MaximumWidthRequest);

    /// <summary>Ferme la liste ; la prochaine ouverture repart de la largeur minimale.</summary>
    public void Hide()
    {
        IsVisible = false;
        _width = MinPopupWidth;
    }

    /// <summary>Remplace la liste en gardant la suggestion courante si elle est encore presente.</summary>
    public void SetItems(IReadOnlyList<CompletionItem> items)
    {
        var previous = SelectedItem;

        _items = items;
        HeightRequest = Math.Min(items.Count, VisibleRows) * RowHeight + Chrome;

        var index = 0;
        if (previous is not null)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (ReferenceEquals(items[i], previous))
                {
                    index = i;
                    break;
                }
            }
        }

        _selectedIndex = items.Count == 0 ? -1 : index;
        _offset = Math.Clamp(_offset, 0, MaxOffset);
        EnsureVisible(_selectedIndex);
        Render();
    }

    public void MoveSelection(int delta)
    {
        if (_items.Count == 0)
            return;

        _selectedIndex = Math.Clamp(_selectedIndex + delta, 0, _items.Count - 1);
        EnsureVisible(_selectedIndex);
        Render();
    }

    private int MaxOffset => Math.Max(0, _items.Count - VisibleRows);

    /// <summary>Ne defile que si la ligne sort de la fenetre visible.</summary>
    private void EnsureVisible(int index)
    {
        if (index < 0)
            return;

        if (index < _offset)
            _offset = index;
        else if (index >= _offset + VisibleRows)
            _offset = index - VisibleRows + 1;
    }

    private void Scroll(int rows)
    {
        var offset = Math.Clamp(_offset + rows, 0, MaxOffset);
        if (offset == _offset)
            return;

        _offset = offset;
        Render();
    }

    /// <summary>Seules les proprietes qui changent sont reecrites : les bindings ne se relancent que pour elles.</summary>
    private void Render()
    {
        for (var i = 0; i < Rows.Children.Count; i++)
        {
            var index = _offset + i;
            var row = (CompletionRow)Rows.Children[i];
            var item = index < _items.Count ? _items[index] : null;

            if (!ReferenceEquals(row.Item, item))
                row.Item = item;

            row.IsSelected = item is not null && index == _selectedIndex;
        }

        UpdateWidth();
        UpdateThumb();
    }

    /// <summary>
    /// Largeur du plus long element visible. Mesure sans limite : une colonne * mesuree dans une largeur
    /// finie s'etirerait jusqu'au maximum. MaximumWidthRequest tronque ensuite le libelle.
    /// </summary>
    private void UpdateWidth()
    {
        foreach (var row in Rows.Children)
            _width = Math.Max(_width, row.Measure(double.PositiveInfinity, RowHeight).Width + Chrome);

        WidthRequest = _width;
    }

    private void UpdateThumb()
    {
        if (_items.Count <= VisibleRows)
        {
            ScrollThumb.IsVisible = false;
            return;
        }

        var track = VisibleRows * RowHeight;
        var height = Math.Max(12, track * VisibleRows / _items.Count);
        ScrollThumb.HeightRequest = height;
        ScrollThumb.TranslationY = (track - height) * _offset / MaxOffset;
        ScrollThumb.IsVisible = true;
    }

    private void OnRowTapped(object? sender, TappedEventArgs e)
    {
        if (sender is CompletionRow { Item: { } item })
            ItemInvoked?.Invoke(this, item);
    }

#if WINDOWS
    private Microsoft.UI.Xaml.UIElement? _wheelTarget;

    /// <summary>Plus de ScrollView : la molette est lue directement sur la vue native.</summary>
    private void AttachWheel()
    {
        if (_wheelTarget is not null)
            _wheelTarget.PointerWheelChanged -= OnPointerWheelChanged;

        _wheelTarget = Surface.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;

        if (_wheelTarget is not null)
            _wheelTarget.PointerWheelChanged += OnPointerWheelChanged;
    }

    private void OnPointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(_wheelTarget).Properties.MouseWheelDelta;
        if (delta != 0)
            Scroll(delta > 0 ? -WheelRows : WheelRows);

        // L'editeur dessous ne doit pas defiler en meme temps.
        e.Handled = true;
    }
#endif
}
