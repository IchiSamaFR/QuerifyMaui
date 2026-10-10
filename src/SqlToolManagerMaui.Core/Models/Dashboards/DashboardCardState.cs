using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Models.Queries;
using System.Runtime.CompilerServices;

namespace SqlToolManagerMaui.Core.Models.Dashboards;

/// <summary>
/// Carte affichee : sa definition (modifiee par le concepteur, directement dans le fichier)
/// et le resultat de sa derniere lecture.
/// </summary>
public sealed partial class DashboardCardState : ObservableObject
{
    private const string NULL_TEXT = "NULL";
    private const string NO_VALUE_TEXT = "—";

    private string _navigationParametersText;

    public DashboardCardDefinition Definition { get; }

    /// <summary>Le concepteur a modifie la definition (position, requete, rendu...).</summary>
    public event EventHandler? DefinitionChanged;

    /// <summary>Bouton presse, ou ligne cliquee d'un tableau qui ouvre une page.</summary>
    public event EventHandler<DashboardNavigationRequest>? NavigationRequested;

    public DashboardCardState(DashboardCardDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Definition = definition;
        _navigationParametersText = definition.Navigation?.FormatParameters() ?? string.Empty;
    }

    // ---------- Definition (concepteur) ----------

    public int Column
    {
        get => Definition.Column;
        set => Update(Definition.Column, Math.Max(0, value), v => Definition.Column = v);
    }

    public int Row
    {
        get => Definition.Row;
        set => Update(Definition.Row, Math.Max(0, value), v => Definition.Row = v);
    }

    public int ColumnSpan
    {
        get => Definition.ColumnSpan;
        set => Update(Definition.ColumnSpan, Math.Max(1, value), v => Definition.ColumnSpan = v);
    }

    public int RowSpan
    {
        get => Definition.RowSpan;
        set => Update(Definition.RowSpan, Math.Max(1, value), v => Definition.RowSpan = v);
    }

    public string Title
    {
        get => Definition.Title ?? string.Empty;
        set => Update(Definition.Title, string.IsNullOrWhiteSpace(value) ? null : value, v => Definition.Title = v);
    }

    public DashboardCardKind View
    {
        get => Definition.View;
        set => Update(Definition.View, value, v => Definition.View = v);
    }

    public string Query
    {
        get => Definition.Query ?? string.Empty;
        set => Update(Definition.Query, string.IsNullOrWhiteSpace(value) ? null : value, v => Definition.Query = v);
    }

    public string Text
    {
        get => Definition.Text ?? string.Empty;
        set => Update(Definition.Text, string.IsNullOrEmpty(value) ? null : value, v => Definition.Text = v);
    }

    public string LabelColumn
    {
        get => Definition.LabelColumn ?? string.Empty;
        set => Update(Definition.LabelColumn, string.IsNullOrWhiteSpace(value) ? null : value.Trim(), v => Definition.LabelColumn = v);
    }

    /// <summary>Colonnes de valeurs separees par des virgules.</summary>
    public string ValueColumnsText
    {
        get => string.Join(", ", Definition.ValueColumns);
        set
        {
            var columns = (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (columns.SequenceEqual(Definition.ValueColumns, StringComparer.Ordinal))
                return;

            Definition.ValueColumns.Clear();
            Definition.ValueColumns.AddRange(columns);
            Changed();
        }
    }

    /// <summary>Page ouverte par le bouton ou le clic sur une ligne ; vide = aucune.</summary>
    public string NavigationPage
    {
        get => Definition.Navigation?.Page ?? string.Empty;
        set
        {
            var page = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (string.Equals(page, Definition.Navigation?.Page, StringComparison.Ordinal))
                return;

            Definition.Navigation = page is null
                ? null
                : new DashboardNavigationDefinition { Page = page, Parameters = DashboardNavigationDefinition.ParseParameters(_navigationParametersText) };
            Changed();
        }
    }

    /// <summary>Parametres transmis, un par ligne : « ShowId = Id » (colonne), « = @UserId » ou « = 'texte' ».</summary>
    public string NavigationParametersText
    {
        get => _navigationParametersText;
        set
        {
            if (string.Equals(_navigationParametersText, value, StringComparison.Ordinal))
                return;

            _navigationParametersText = value ?? string.Empty;
            if (Definition.Navigation is { } navigation)
            {
                navigation.Parameters.Clear();
                navigation.Parameters.AddRange(DashboardNavigationDefinition.ParseParameters(_navigationParametersText));
            }
            Changed();
        }
    }

    // ---------- Rendu ----------

    public bool HasTitle => !string.IsNullOrWhiteSpace(Definition.Title);

    public bool IsGrid => Definition.View == DashboardCardKind.Grid;

    public bool IsChart => Definition.View is DashboardCardKind.BarChart or DashboardCardKind.LineChart or DashboardCardKind.PieChart;

    public bool IsValue => Definition.View == DashboardCardKind.Value;

    public bool IsText => Definition.View == DashboardCardKind.Text;

    public bool IsButton => Definition.View == DashboardCardKind.Button;

    /// <summary>Carte alimentee par une requete (ni texte, ni bouton).</summary>
    public bool UsesQuery => Definition.View is not (DashboardCardKind.Text or DashboardCardKind.Button);

    public bool HasNavigation => Definition.Navigation is not null;

    public string ButtonText => Definition.Text ?? Definition.Title ?? Definition.Navigation?.Page ?? "Ouvrir";

    /// <summary>Carte Valeur : premiere ligne, premiere colonne de valeurs (ou premiere colonne).</summary>
    public string ValueText
    {
        get
        {
            if (Result is not { Rows.Count: > 0 } result)
                return NO_VALUE_TEXT;

            var column = Definition.ValueColumns.FirstOrDefault();
            var index = column is null ? 0 : result.Columns.ToList().FindIndex(c => string.Equals(c.Name, column, StringComparison.OrdinalIgnoreCase));
            var cells = result.Rows[0].Cells;
            return index >= 0 && index < cells.Length ? cells[index] ?? NULL_TEXT : NO_VALUE_TEXT;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValueText), nameof(HasResult))]
    public partial QueryResultSet? Result { get; set; }

    public bool HasResult => Result is not null;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? Error { get; set; }

    public bool HasError => Error is not null;

    /// <summary>Carte selectionnee dans le concepteur.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    // ---------- Navigation ----------

    [RelayCommand]
    private void ActivateRow(QueryResultRow? row)
    {
        if (HasNavigation && row is not null)
            NavigationRequested?.Invoke(this, new DashboardNavigationRequest(this, row));
    }

    [RelayCommand]
    private void Press()
    {
        if (HasNavigation)
            NavigationRequested?.Invoke(this, new DashboardNavigationRequest(this, Row: null));
    }

    private void Update<T>(T current, T value, Action<T> apply, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
            return;

        apply(value);
        OnPropertyChanged(propertyName);
        Changed();
    }

    /// <summary>Les proprietes derivees suivent, puis l'hote reecrit le fichier.</summary>
    private void Changed()
    {
        foreach (var name in DerivedProperties)
            OnPropertyChanged(name);

        DefinitionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static readonly string[] DerivedProperties =
    [
        nameof(HasTitle), nameof(IsGrid), nameof(IsChart), nameof(IsValue), nameof(IsText), nameof(IsButton),
        nameof(UsesQuery), nameof(HasNavigation), nameof(ButtonText), nameof(ValueText),
        nameof(ValueColumnsText), nameof(NavigationPage), nameof(LabelColumn),
    ];
}
