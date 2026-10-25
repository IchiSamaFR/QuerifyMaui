using Jint;
using Jint.Native;
using SqlToolManagerMaui.Databases.Models.Queries;

namespace SqlToolManagerMaui.Languages.JavaScript;

/// <summary>Valeur rendue par le script (return, dump) convertie en grille.</summary>
internal static class JavaScriptResultConverter
{
    private const string ValueColumn = "Valeur";

    public static QueryResultSet ToResultSet(Engine engine, JsValue value, int index, string? title, int maxRows, CancellationToken cancellationToken)
    {
        // Une valeur seule (nombre, objet) s'affiche comme une ligne.
        if (!value.IsArray())
            return Build(engine, [value], isTruncated: false, index, title);

        List<JsValue> rows = [];
        var isTruncated = false;

        foreach (var item in value.AsArray())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (rows.Count == maxRows)
            {
                isTruncated = true;
                break;
            }

            rows.Add(item);
        }

        return Build(engine, rows, isTruncated, index, title);
    }

    private static QueryResultSet Build(Engine engine, List<JsValue> items, bool isTruncated, int index, string? title)
    {
        if (items.Exists(IsRecord) && items.TrueForAll(i => IsMissing(i) || IsRecord(i)))
            return BuildFromRecords(engine, items, isTruncated, index, title);

        var sample = items.Find(i => !IsMissing(i));
        var column = new QueryResultColumn(ValueColumn, sample is null ? "null" : JavaScriptValueFormatter.TypeName(sample));
        var rows = items.Select((item, i) => new QueryResultRow(i + 1, [JavaScriptValueFormatter.ToCell(engine, item)])).ToArray();

        return new QueryResultSet(index, [column], rows, isTruncated, title);
    }

    // Colonnes : union ordonnee des cles de toutes les lignes (une cle absente = cellule vide).
    private static QueryResultSet BuildFromRecords(Engine engine, List<JsValue> items, bool isTruncated, int index, string? title)
    {
        var records = items.Select(Fields).ToList();

        List<string> names = [];
        HashSet<string> seen = [];
        Dictionary<string, string> types = [];
        foreach (var field in records.SelectMany(r => r))
        {
            if (seen.Add(field.Key))
                names.Add(field.Key);

            if (!IsMissing(field.Value))
                types.TryAdd(field.Key, JavaScriptValueFormatter.TypeName(field.Value));
        }

        var columns = names.Select(n => new QueryResultColumn(n, types.GetValueOrDefault(n, "null"))).ToArray();
        var rows = records
            .Select((record, i) =>
            {
                var values = record.ToDictionary(f => f.Key, f => f.Value);
                return new QueryResultRow(i + 1, names.Select(n => values.TryGetValue(n, out var v) ? JavaScriptValueFormatter.ToCell(engine, v) : null).ToArray());
            })
            .ToArray();

        return new QueryResultSet(index, columns, rows, isTruncated, title);
    }

    private static List<KeyValuePair<string, JsValue>> Fields(JsValue record)
    {
        if (IsMissing(record))
            return [];

        var instance = record.AsObject();
        return instance.GetOwnProperties()
            .Where(p => p.Key.IsString() && p.Value.Enumerable)
            .Select(p => new KeyValuePair<string, JsValue>(p.Key.AsString(), instance.Get(p.Key)))
            .ToList();
    }

    private static bool IsMissing(JsValue value) => value.IsNull() || value.IsUndefined();

    private static bool IsRecord(JsValue value)
        => value.IsObject() && !value.IsArray() && !value.IsDate() && !value.IsCallable() && !value.IsRegExp();
}
