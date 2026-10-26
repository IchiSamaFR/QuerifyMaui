using Querify.Helpers.Tools;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Querify.UI.Views.Controls.DataGrids;

/// <summary>
/// Grille de donnees facon WPF : colonnes declarees (<see cref="Columns"/>) et/ou generees depuis les proprietes
/// des elements (<see cref="AutoGenerateColumns"/>), tri au clic sur un titre, filtre par colonne via le bouton de son en-tete.
/// Lignes virtualisees remplies au recyclage ; en-tetes fixes, defilement horizontal si besoin.
///
/// Exemple :
/// <![CDATA[
///   <v:DataGrid ItemsSource="{Binding Customers}"
///               SelectionMode="Single"
///               SelectedItem="{Binding SelectedCustomer}" />
///
///   <v:DataGrid ItemsSource="{Binding Orders}"
///               AutoGenerateColumns="False">
///       <v:DataGrid.Columns>
///           <v:DataGridColumn Header="Date" Path="OrderDate" StringFormat="dd/MM/yyyy" />
///           <v:DataGridColumn Header="Total" Path="Total" StringFormat="C2" />
///       </v:DataGrid.Columns>
///   </v:DataGrid>
/// ]]>
/// </summary>
public partial class DataGrid : ContentView
{
    /// <summary>Largeur de la saisie de filtre.</summary>
    public const double FilterPopupWidth = 260;

    private const double MinColumnWidth = 72;
    private const double MaxColumnWidth = 360;
    private const double ApproximateCharWidth = 7.5;
    private const double CellHorizontalPadding = 24;
    private const double SortGlyphWidth = 14;
    private const double FilterButtonWidth = 22;

    /// <summary>Nombre de lignes echantillonnees pour dimensionner les colonnes.</summary>
    private const int WidthSampleRowCount = 200;

    /// <summary>Pause de frappe avant d'appliquer un filtre.</summary>
    private static readonly TimeSpan FilterDelay = TimeSpan.FromMilliseconds(250);

    private readonly List<DataGridColumn> _columns = [];
    private readonly List<DataGridColumn> _generatedColumns = [];
    private readonly List<DataGridHeaderCell> _headerCells = [];
    private double[] _columnOffsets = [];
    private List<object> _rows = [];

    /// <summary>Position affichee de chaque ligne (apres filtre et tri) : une ligne sur deux est assombrie.</summary>
    private Dictionary<object, int> _rowIndexes = new(ReferenceEqualityComparer.Instance);

    /// <summary>Faible : les lignes recyclees par la liste s'abonnent sans etre retenues par la grille.</summary>
    private readonly WeakEventManager _rowsReorderedManager = new();
    private Type? _generatedItemType;
    private INotifyCollectionChanged? _observedItems;
    private INotifyCollectionChanged? _observedColumns;
    private IDispatcherTimer? _filterTimer;
    private bool _isRefreshPending;
    private bool _areColumnsDirty;
    private bool _isSyncingSelection;

    // Vrai pendant un report entre l'etat bindable (SortMember, SortDirection, Filters) et les colonnes.
    private bool _isSyncingState;

    public DataGrid()
    {
        InitializeComponent();

        // Collection par defaut creee sans notification : elle est suivie ici (colonnes ajoutees en XAML).
        OnColumnsChanged(Columns);
    }

    #region ---- Proprietes ----
    private static object CreateDefaultColumns(BindableObject bindable)
        => new ObservableCollection<DataGridColumn>();

    private static object CreateSortCommand(BindableObject bindable)
        => new Command<DataGridColumn>(((DataGrid)bindable).ToggleSort);

    private static object CreateFilterCommand(BindableObject bindable)
        => new Command<DataGridColumn>(((DataGrid)bindable).ToggleFilterEditor);

    private static object CreateCloseFilterCommand(BindableObject bindable)
        => new Command(((DataGrid)bindable).CloseFilterEditor);

    private static object CreateClearFiltersCommand(BindableObject bindable)
        => new Command(((DataGrid)bindable).ClearFilters);

    private static void OnItemsSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DataGrid)bindable).OnItemsSourceChanged(newValue as IEnumerable);

    private static void OnColumnsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DataGrid)bindable).OnColumnsChanged(newValue as IList<DataGridColumn>);

    private static void OnLayoutPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DataGrid)bindable).ScheduleRefresh(columnsChanged: true);

    private static void OnSelectionBehaviorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DataGrid)bindable).ApplySelectionMode();

    private static void OnSelectedItemPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DataGrid)bindable).SelectDisplayedRow();

    private static void OnSortStatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DataGrid)bindable).OnSortStateChanged();

    private static void OnFiltersPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DataGrid)bindable).OnFiltersChanged();

    private void OnItemsSourceChanged(IEnumerable? newSource)
    {
        if (_observedItems is not null)
            _observedItems.CollectionChanged -= OnItemsCollectionChanged;

        _observedItems = newSource as INotifyCollectionChanged;
        if (_observedItems is not null)
            _observedItems.CollectionChanged += OnItemsCollectionChanged;

        // Colonnes generees a revoir seulement si le type des elements change (les filtres sont conserves sinon).
        ScheduleRefresh(columnsChanged: AutoGenerateColumns && DataGridColumnFactory.ResolveItemType(newSource) != _generatedItemType);
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => ScheduleRefresh(columnsChanged: AutoGenerateColumns && _generatedItemType is null);

    private void OnColumnsChanged(IList<DataGridColumn>? columns)
    {
        if (_observedColumns is not null)
            _observedColumns.CollectionChanged -= OnColumnsCollectionChanged;

        _observedColumns = columns as INotifyCollectionChanged;
        if (_observedColumns is not null)
            _observedColumns.CollectionChanged += OnColumnsCollectionChanged;

        ScheduleRefresh(columnsChanged: true);
    }

    private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => ScheduleRefresh(columnsChanged: true);

    private void OnColumnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not DataGridColumn column)
            return;

        // Etat reporte depuis les proprietes de la grille : le rafraichissement est deja demande.
        if (_isSyncingState && e.PropertyName is nameof(DataGridColumn.FilterText) or nameof(DataGridColumn.SortDirection))
            return;

        switch (e.PropertyName)
        {
            case nameof(DataGridColumn.FilterText):
                RestartFilterTimer();
                break;

            case nameof(DataGridColumn.SortDirection):
                OnColumnSorted(column);
                break;

            // CanFilter : le bouton de filtre de l'en-tete apparait ou disparait.
            case nameof(DataGridColumn.CanFilter) or nameof(DataGridColumn.Width) or nameof(DataGridColumn.Path) or nameof(DataGridColumn.StringFormat):
                ScheduleRefresh(columnsChanged: true);
                break;
        }
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        // Les colonnes XAML peuvent se lier au contexte de la page.
        foreach (var column in _columns)
            SetInheritedBindingContext(column, BindingContext);
    }
    #endregion

    #region ---- Rafraichissement ----
    // Regroupe les changements d'un meme cycle (ItemsSource et Columns changent souvent ensemble).
    private void ScheduleRefresh(bool columnsChanged)
    {
        _areColumnsDirty |= columnsChanged;
        if (_isRefreshPending)
            return;

        _isRefreshPending = true;
        Dispatcher.Dispatch(Refresh);
    }

    private void RestartFilterTimer()
    {
        if (_filterTimer is null)
        {
            _filterTimer = Dispatcher.CreateTimer();
            _filterTimer.Interval = FilterDelay;
            _filterTimer.IsRepeating = false;
            _filterTimer.Tick += (_, _) => ScheduleRefresh(columnsChanged: false);
        }

        _filterTimer.Stop();
        _filterTimer.Start();
    }

    private void Refresh()
    {
        _isRefreshPending = false;
        _filterTimer?.Stop();

        if (_areColumnsDirty)
        {
            _areColumnsDirty = false;
            RebuildColumns();
        }

        RefreshRows();
    }

    private void RebuildColumns()
    {
        // Lignes videes avant le changement de gabarit : les lignes recyclees portent les anciennes largeurs.
        SetRowsSource(null);
        CloseFilterEditor();

        foreach (var column in _columns)
            column.PropertyChanged -= OnColumnPropertyChanged;

        _columns.Clear();
        if (Columns is { } declared)
            _columns.AddRange(declared);

        if (AutoGenerateColumns)
        {
            UpdateGeneratedColumns();
            _columns.AddRange(_generatedColumns);
        }

        RestoreStateOnColumns();

        foreach (var column in _columns)
        {
            column.PropertyChanged += OnColumnPropertyChanged;
            SetInheritedBindingContext(column, BindingContext);
        }

        foreach (var cell in _headerCells)
            HeaderRow.Remove(cell);

        _headerCells.Clear();
        HeaderRow.ColumnDefinitions.Clear();

        HasColumns = _columns.Count > 0;
        if (!HasColumns)
            return;

        var sample = EnumerateItems().Take(WidthSampleRowCount).ToList();
        var widths = _columns.Select(column => MeasureColumn(column, sample)).ToArray();
        _columnOffsets = new double[_columns.Count];
        var offset = 0d;

        for (var i = 0; i < _columns.Count; i++)
        {
            HeaderRow.ColumnDefinitions.Add(new ColumnDefinition(widths[i]));
            _columnOffsets[i] = offset;
            offset += widths[i] + HeaderRow.ColumnSpacing;

            var header = new DataGridHeaderCell
            {
                Column = _columns[i],
                SortCommand = SortCommand,
                FilterCommand = FilterCommand,
                IsFilterButtonVisible = CanUserFilter && _columns[i].CanFilter,
            };
            HeaderRow.Add(header, i);
            _headerCells.Add(header);
        }

        // Un espacement de 1 entre deux colonnes, comme dans les lignes.
        Rows.WidthRequest = widths.Sum() + widths.Length - 1;

        var columns = _columns.ToArray();
        var nullText = NullValueText;
        Rows.ItemTemplate = new DataTemplate(() => new DataGridRowView(this, columns, widths, nullText));
    }

    private void UpdateGeneratedColumns()
    {
        var itemType = DataGridColumnFactory.ResolveItemType(ItemsSource);
        if (itemType == _generatedItemType)
            return;

        _generatedItemType = itemType;
        _generatedColumns.Clear();
        if (itemType is not null)
            _generatedColumns.AddRange(DataGridColumnFactory.Create(itemType));
    }

    private void RefreshRows()
    {
        if (!HasColumns)
        {
            SetRowsSource(null);
            UpdateCounts(total: 0, displayed: 0, isFiltered: false);
            return;
        }

        var filters = _columns
            .Where(column => column.CanFilter)
            .Select(column => (Column: column, Filter: TextFilter.Parse(column.FilterText)))
            .Where(f => !f.Filter.IsEmpty)
            .ToArray();

        var rows = new List<object>();
        var total = 0;
        foreach (var item in EnumerateItems())
        {
            total++;
            if (filters.All(f => Matches(f.Column, f.Filter, item)))
                rows.Add(item);
        }

        if (_columns.Find(column => column.SortDirection != DataGridSortDirection.None) is { } sortColumn)
            rows = Sort(rows, sortColumn);

        _rows = rows;
        _rowIndexes = new Dictionary<object, int>(rows.Count, ReferenceEqualityComparer.Instance);
        for (var i = 0; i < rows.Count; i++)
            _rowIndexes.TryAdd(rows[i], i);

        SetRowsSource(rows);

        // Une ligne qui garde le meme element apres un tri ne change pas de contexte : elle est prevenue.
        _rowsReorderedManager.HandleEvent(this, EventArgs.Empty, nameof(RowsReordered));
        UpdateCounts(total, rows.Count, filters.Length > 0);
        PublishFilters();
        SelectDisplayedRow();
    }

    private static bool Matches(DataGridColumn column, TextFilter filter, object item)
    {
        var value = column.ReadValue(item);
        return filter.Matches(value, column.FormatValue(value));
    }

    // Cles normalisees une seule fois par ligne (OrderBy les calcule avant de trier) ; tri stable.
    private static List<object> Sort(List<object> rows, DataGridColumn column)
    {
        var comparer = Comparer<object?>.Create(ValueComparison.Compare);
        object? KeySelector(object item) => ValueComparison.Normalize(column.ReadValue(item));

        return column.SortDirection == DataGridSortDirection.Descending
            ? [.. rows.OrderByDescending(KeySelector, comparer)]
            : [.. rows.OrderBy(KeySelector, comparer)];
    }

    private void UpdateCounts(int total, int displayed, bool isFiltered)
    {
        ItemsCount = total;
        DisplayedItemsCount = displayed;
        IsFiltered = isFiltered;
        HasNoMatchingItems = isFiltered && displayed == 0 && total > 0;
    }

    private IEnumerable<object> EnumerateItems()
        => ItemsSource?.Cast<object?>().OfType<object>() ?? [];

    private double MeasureColumn(DataGridColumn column, List<object> sample)
    {
        if (column.Width > 0)
            return column.Width;

        var headerWidth = (column.Header.Length * ApproximateCharWidth) + SortGlyphWidth
            + (CanUserFilter && column.CanFilter ? FilterButtonWidth : 0);
        var longestCell = sample
            .Select(item => (column.FormatValue(column.ReadValue(item)) ?? NullValueText).Length)
            .DefaultIfEmpty(0)
            .Max();

        var width = Math.Max(headerWidth, longestCell * ApproximateCharWidth) + CellHorizontalPadding;
        return Math.Clamp(width, MinColumnWidth, MaxColumnWidth);
    }
    #endregion

    #region ---- Tri, filtres ----
    private void ToggleSort(DataGridColumn? column)
    {
        if (column is null || !CanUserSort || !column.CanSort)
            return;

        column.SortDirection = column.SortDirection switch
        {
            DataGridSortDirection.None => DataGridSortDirection.Ascending,
            DataGridSortDirection.Ascending => DataGridSortDirection.Descending,
            _ => DataGridSortDirection.None,
        };
    }

    private void ToggleFilterEditor(DataGridColumn? column)
    {
        if (column is null || column == EditedFilterColumn)
        {
            CloseFilterEditor();
            return;
        }

        var index = _columns.IndexOf(column);
        if (index < 0)
            return;

        // Sous l'en-tete de la colonne, ramenee a gauche si elle depasserait la grille.
        var gridWidth = Rows.WidthRequest > 0 ? Rows.WidthRequest : FilterPopupWidth;
        var left = Math.Max(0, Math.Min(_columnOffsets[index], gridWidth - FilterPopupWidth));
        FilterPopup.Margin = new Thickness(left, 0, 0, 0);

        EditedFilterColumn = column;
        IsFilterEditorOpen = true;

#if WINDOWS
        AttachOutsideClick();
#endif

        // Apres l'affichage : une vue masquee ne prend pas le focus.
        Dispatcher.Dispatch(FilterPopup.FocusInput);
    }

    private void CloseFilterEditor()
    {
        if (!IsFilterEditorOpen)
            return;

        IsFilterEditorOpen = false;
        EditedFilterColumn = null;

#if WINDOWS
        DetachOutsideClick();
#endif

        // Le filtre saisi s'applique sans attendre la pause de frappe.
        ScheduleRefresh(columnsChanged: false);
    }

    public void ClearFilters()
    {
        foreach (var column in _columns)
            column.FilterText = string.Empty;

        // Sans attendre la pause de frappe.
        ScheduleRefresh(columnsChanged: false);
    }
    #endregion

    #region ---- Etat bindable (tri, filtres) ----
    private void OnSortStateChanged()
    {
        if (_isSyncingState)
            return;

        WriteSortToColumns();
        ScheduleRefresh(columnsChanged: false);
    }

    private void OnFiltersChanged()
    {
        if (_isSyncingState)
            return;

        WriteFiltersToColumns();
        ScheduleRefresh(columnsChanged: false);
    }

    /// <summary>Tri change depuis un en-tete (ou une colonne) : une seule colonne triee, etat publie.</summary>
    private void OnColumnSorted(DataGridColumn column)
    {
        _isSyncingState = true;
        if (column.SortDirection != DataGridSortDirection.None)
        {
            foreach (var other in _columns.Where(c => c != column))
                other.SortDirection = DataGridSortDirection.None;
        }

        PublishSort();
        _isSyncingState = false;

        ScheduleRefresh(columnsChanged: false);
    }

    /// <summary>
    /// Nouvelles colonnes : elles reprennent l'etat de la grille. Sans etat (premier affichage),
    /// c'est l'etat declare sur les colonnes qui est publie.
    /// </summary>
    private void RestoreStateOnColumns()
    {
        _isSyncingState = true;

        if (SortMember is null)
            PublishSort();
        else
            WriteSortToColumns();

        if (Filters is null)
            PublishFilters();
        else
            WriteFiltersToColumns();

        _isSyncingState = false;
    }

    private void WriteSortToColumns()
    {
        var wasSyncing = _isSyncingState;
        _isSyncingState = true;
        foreach (var column in _columns)
        {
            column.SortDirection = SortMember is not null && column.Key == SortMember
                ? SortDirection
                : DataGridSortDirection.None;
        }

        _isSyncingState = wasSyncing;
    }

    private void WriteFiltersToColumns()
    {
        var wasSyncing = _isSyncingState;
        _isSyncingState = true;
        foreach (var column in _columns)
        {
            column.FilterText = Filters is not null && Filters.TryGetValue(column.Key, out var text)
                ? text
                : string.Empty;
        }

        _isSyncingState = wasSyncing;
    }

    private void PublishSort()
    {
        var sorted = _columns.Find(c => c.SortDirection != DataGridSortDirection.None);

        var wasSyncing = _isSyncingState;
        _isSyncingState = true;
        SortMember = sorted?.Key;
        SortDirection = sorted?.SortDirection ?? DataGridSortDirection.None;
        _isSyncingState = wasSyncing;
    }

    /// <summary>Publie les filtres saisis, seulement s'ils different (un nouveau dictionnaire a chaque changement).</summary>
    private void PublishFilters()
    {
        var filters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var column in _columns.Where(c => !string.IsNullOrWhiteSpace(c.FilterText)))
            filters.TryAdd(column.Key, column.FilterText);

        var current = Filters ?? new Dictionary<string, string>();
        var isSame = current.Count == filters.Count
            && filters.All(f => current.TryGetValue(f.Key, out var text) && text == f.Value);
        if (isSame)
            return;

        var wasSyncing = _isSyncingState;
        _isSyncingState = true;
        Filters = filters;
        _isSyncingState = wasSyncing;
    }
    #endregion

    #region ---- Selection ----
    private void ApplySelectionMode()
    {
        // Sans selection, une commande de clic a besoin d'une selection native ephemere.
        Rows.SelectionMode = SelectionMode == SelectionMode.None && ItemTappedCommand is not null
            ? SelectionMode.Single
            : SelectionMode;
    }

    /// <summary>Lignes refiltrees ou retriees : chacune recalcule sa position.</summary>
    internal event EventHandler RowsReordered
    {
        add => _rowsReorderedManager.AddEventHandler(value);
        remove => _rowsReorderedManager.RemoveEventHandler(value);
    }

    /// <summary>Position affichee de l'element, -1 s'il n'est pas affiche.</summary>
    internal int IndexOfRow(object item) => _rowIndexes.TryGetValue(item, out var index) ? index : -1;

    private void SetRowsSource(IEnumerable? rows)
    {
        // Le remplacement des lignes vide la selection native : SelectedItem ne doit pas suivre.
        _isSyncingSelection = true;
        Rows.ItemsSource = rows;
        _isSyncingSelection = false;
    }

    private void OnRowsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingSelection)
            return;

        var item = e.CurrentSelection.FirstOrDefault();

        // Sans selection, deselection immediate : un nouveau clic sur la meme ligne doit relancer la commande.
        _isSyncingSelection = true;
        if (SelectionMode == SelectionMode.None)
        {
            Rows.SelectedItem = null;
        }
        else
        {
            SelectedItem = item;
        }

        _isSyncingSelection = false;

        if (item is not null && ItemTappedCommand is { } command && command.CanExecute(item))
            command.Execute(item);
    }

    /// <summary>Reporte <see cref="SelectedItem"/> sur la ligne affichee ; un element filtre ou disparu est deselectionne.</summary>
    private void SelectDisplayedRow()
    {
        if (_isSyncingSelection || SelectionMode == SelectionMode.None)
            return;

        var item = SelectedItem is null ? null : _rows.Find(r => Equals(r, SelectedItem));

        _isSyncingSelection = true;
        Rows.SelectedItem = item;
        if (item is null && SelectedItem is not null && Rows.ItemsSource is not null)
            SelectedItem = null;
        _isSyncingSelection = false;
    }
    #endregion

#if WINDOWS
    #region ---- Clic exterieur (WinUI) ----
    private global::Microsoft.UI.Xaml.UIElement? _windowContent;
    private global::Microsoft.UI.Xaml.Input.PointerEventHandler? _windowPointerPressedHandler;

    // Ecoute au niveau de la fenetre (evenements deja traites compris) : tout clic hors de la saisie la ferme.
    private void AttachOutsideClick()
    {
        if (_windowContent is not null)
            return;

        _windowContent = (Window?.Handler?.PlatformView as global::Microsoft.UI.Xaml.Window)?.Content;
        if (_windowContent is null)
            return;

        _windowPointerPressedHandler ??= OnWindowPointerPressed;
        _windowContent.AddHandler(global::Microsoft.UI.Xaml.UIElement.PointerPressedEvent, _windowPointerPressedHandler, handledEventsToo: true);
    }

    private void DetachOutsideClick()
    {
        if (_windowContent is not null && _windowPointerPressedHandler is not null)
            _windowContent.RemoveHandler(global::Microsoft.UI.Xaml.UIElement.PointerPressedEvent, _windowPointerPressedHandler);

        _windowContent = null;
    }

    private void OnWindowPointerPressed(object sender, global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!IsFilterEditorOpen)
            return;

        // Le bouton de filtre de la colonne referme lui-meme la saisie a son clic : la fermer ici la rouvrirait.
        if (IsPointerOver(FilterPopup, e) || IsOnFilterButtonOf(EditedFilterColumn, e))
            return;

        CloseFilterEditor();
    }

    private static bool IsPointerOver(VisualElement element, global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (element.Handler?.PlatformView is not global::Microsoft.UI.Xaml.FrameworkElement native)
            return false;

        var position = e.GetCurrentPoint(native).Position;
        return position.X >= 0 && position.Y >= 0 && position.X <= native.ActualWidth && position.Y <= native.ActualHeight;
    }

    /// <summary>Vrai si le pointeur est sur un bouton de l'en-tete de la colonne (son bouton de filtre).</summary>
    private bool IsOnFilterButtonOf(DataGridColumn? column, global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_headerCells.Find(cell => cell.Column == column)?.Handler?.PlatformView is not global::Microsoft.UI.Xaml.DependencyObject header)
            return false;

        var isOnButton = false;
        var node = e.OriginalSource as global::Microsoft.UI.Xaml.DependencyObject;
        while (node is not null && node != header)
        {
            isOnButton |= node is global::Microsoft.UI.Xaml.Controls.Primitives.ButtonBase;
            node = global::Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(node);
        }

        return isOnButton && node == header;
    }
    #endregion
#endif
}
