using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Querify.Core.Enums.Dashboards;
using Querify.Core.Enums.Editors.Highlighting;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Quill.Code.Sql.Execution;

namespace Querify.Core.Models.Dashboards;

/// <summary>
/// Carte affichee : sa definition (modifiee par le concepteur, directement dans le fichier)
/// et le resultat de sa derniere lecture.
/// </summary>
public sealed partial class DashboardCardState : ObservableObject
{
    private const string NULL_TEXT = "NULL";
    private const string NO_VALUE_TEXT = "—";
    private const string FIRST_COLUMN_LABEL = "(première colonne)";

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
        RefreshColumnChoices();
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

    /// <summary>SQL ou LINQ (EF) ; SQL n'est pas ecrit dans le fichier.</summary>
    public SyntaxLanguage Language
    {
        get => Definition.Language ?? SyntaxLanguage.Sql;
        set => Update(Definition.Language, value == SyntaxLanguage.Linq ? value : (SyntaxLanguage?)null, v => Definition.Language = v);
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

    // ---------- Choix des colonnes (concepteur) ----------

    /// <summary>
    /// Colonnes des libelles pour un picker : cle "" = la premiere colonne, puis celles du dernier resultat.
    /// Une colonne choisie mais absente du resultat reste proposee, pour ne pas perdre le choix avant execution.
    /// </summary>
    public Dictionary<string, string> LabelColumnChoices { get; private set; } = new() { [string.Empty] = FIRST_COLUMN_LABEL };

    /// <summary>Colonnes de valeurs, cochees dans l'ordre du choix.</summary>
    public ObservableCollection<DashboardColumnChoice> ValueColumnChoices { get; } = [];

    /// <summary>Ajoute la colonne a la fin des valeurs, ou l'en retire.</summary>
    [RelayCommand]
    private void ToggleValueColumn(DashboardColumnChoice? choice)
    {
        if (choice is null)
            return;

        var index = Definition.ValueColumns.FindIndex(c => string.Equals(c, choice.Name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            Definition.ValueColumns.RemoveAt(index);
        else
            Definition.ValueColumns.Add(choice.Name);

        Changed();
    }

    partial void OnResultChanged(QueryResultSet? value)
    {
        RefreshColumnChoices();
        RefreshGridResult();
    }

    // ---------- Tableau ----------

    private QueryResultSet? _gridSource;
    private string? _gridColumnsKey;

    /// <summary>Tableau affiche : les colonnes de valeurs choisies, dans leur ordre ; tout le resultat si aucune ne s'y trouve.</summary>
    public QueryResultSet? GridResult { get; private set; }

    /// <summary>Recalcule seulement si le resultat ou les colonnes changent : pas a chaque frappe dans la requete.</summary>
    private void RefreshGridResult()
    {
        var key = string.Join('\u001f', Definition.ValueColumns);
        if (ReferenceEquals(_gridSource, Result) && key == _gridColumnsKey)
            return;

        _gridSource = Result;
        _gridColumnsKey = key;
        GridResult = Project(Result, Definition.ValueColumns);
        OnPropertyChanged(nameof(GridResult));
    }

    /// <summary>Lignes projetees sur les colonnes choisies ; elles gardent leur numero pour retrouver la ligne d'origine.</summary>
    private static QueryResultSet? Project(QueryResultSet? result, IReadOnlyList<string> columns)
    {
        if (result is null || columns.Count == 0)
            return result;

        var indexes = columns
            .Select(name => result.Columns.ToList().FindIndex(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            .Where(index => index >= 0)
            .Distinct()
            .ToArray();

        if (indexes.Length == 0)
            return result;

        var rows = result.Rows
            .Select(row => new QueryResultRow(row.Number, indexes.Select(i => i < row.Cells.Length ? row.Cells[i] : null).ToArray()))
            .ToArray();

        return new QueryResultSet(result.Index, indexes.Select(i => result.Columns[i]).ToArray(), rows, result.IsTruncated, result.Title);
    }

    /// <summary>
    /// Rebatit les listes seulement si les colonnes proposees changent (nouveau resultat) : a chaque frappe
    /// dans la requete, seules les coches suivent, sans recreer les puces ni le picker.
    /// </summary>
    private void RefreshColumnChoices()
    {
        var available = Result?.Columns.Select(c => c.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];
        var configured = Definition.ValueColumns.Append(Definition.LabelColumn ?? string.Empty)
            .Where(c => c.Length > 0 && !available.Contains(c, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var names = available.Concat(configured).ToList();

        if (!names.SequenceEqual(ValueColumnChoices.Select(c => c.Name), StringComparer.Ordinal))
        {
            ValueColumnChoices.Clear();
            foreach (var name in names)
                ValueColumnChoices.Add(new DashboardColumnChoice(name));

            LabelColumnChoices = new Dictionary<string, string> { [string.Empty] = FIRST_COLUMN_LABEL };
            foreach (var name in names)
                LabelColumnChoices.TryAdd(name, name);

            OnPropertyChanged(nameof(LabelColumnChoices));
        }

        foreach (var choice in ValueColumnChoices)
        {
            var order = Definition.ValueColumns.FindIndex(c => string.Equals(c, choice.Name, StringComparison.OrdinalIgnoreCase));
            choice.IsSelected = order >= 0;
            choice.Order = order + 1;
            choice.IsAvailable = Result is null || available.Contains(choice.Name, StringComparer.OrdinalIgnoreCase);
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

    public bool IsLinq => Language == SyntaxLanguage.Linq;

    public bool HasNavigation => Definition.Navigation is not null;

    public string ButtonText => Definition.Text ?? "Ouvrir";

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

    /// <summary>Par la propriete View : le graphique, lie a View, change de forme (barres, lignes, secteurs).</summary>
    [RelayCommand]
    private void SetCardKind(DashboardCardKind kind) => View = kind;


    // ---------- Navigation ----------

    /// <summary>La ligne cliquee peut venir du tableau projete : la navigation lit les colonnes de la ligne complete.</summary>
    [RelayCommand]
    private void ActivateRow(QueryResultRow? row)
    {
        if (!HasNavigation || row is null)
            return;

        var source = Result?.Rows.FirstOrDefault(r => r.Number == row.Number) ?? row;
        NavigationRequested?.Invoke(this, new DashboardNavigationRequest(this, source));
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
        RefreshColumnChoices();
        RefreshGridResult();

        foreach (var name in DerivedProperties)
            OnPropertyChanged(name);

        DefinitionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static readonly string[] DerivedProperties =
    [
        nameof(HasTitle), nameof(IsGrid), nameof(IsChart), nameof(IsValue), nameof(IsText), nameof(IsButton),
        nameof(UsesQuery), nameof(IsLinq), nameof(HasNavigation), nameof(ButtonText), nameof(ValueText),
        nameof(ValueColumnsText), nameof(NavigationPage), nameof(LabelColumn),
    ];
}
