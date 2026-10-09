namespace SqlToolManagerMaui.Core.Models.Queries;

/// <summary>Un jeu de resultats (un SELECT) renvoye par l'execution.</summary>
public sealed class QueryResultSet
{
    /// <summary>Position du jeu dans l'execution (base 1).</summary>
    public int Index { get; }

    public IReadOnlyList<QueryResultColumn> Columns { get; }

    public IReadOnlyList<QueryResultRow> Rows { get; }

    /// <summary>Vrai si la lecture a ete interrompue au plafond de lignes.</summary>
    public bool IsTruncated { get; }

    public string Title => $"Résultats {Index}";

    public string Summary => IsTruncated
        ? $"{Rows.Count} premières lignes"
        : $"{Rows.Count} ligne(s)";

    public QueryResultSet(int index, IReadOnlyList<QueryResultColumn> columns, IReadOnlyList<QueryResultRow> rows, bool isTruncated)
    {
        Index = index;
        Columns = columns;
        Rows = rows;
        IsTruncated = isTruncated;
    }
}
