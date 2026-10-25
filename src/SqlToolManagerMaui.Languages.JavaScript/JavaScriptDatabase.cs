using Jint;
using Jint.Native;
using Jint.Runtime;
using Jint.Runtime.Interop;
using SqlToolManagerMaui.Core.Enums.Queries;
using SqlToolManagerMaui.Databases.Enums;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Databases.Models.Queries;
using SqlToolManagerMaui.Databases.Queries;
using SqlToolManagerMaui.Languages.Commons.Queries;
using System.Globalization;

namespace SqlToolManagerMaui.Languages.JavaScript;

/// <summary>
/// Objet db d'une execution : db.query(sql, { param: valeur }) rend un tableau d'objets.
/// Passe par <see cref="IQueryReader"/> pour rester independant du moteur.
/// </summary>
internal sealed class JavaScriptDatabase
{
    private const int MaxRows = QueryExecutionLimits.MaxRowsPerResultSet;

    /// <summary>Au-dela, un entier n'est plus exact en double : il reste en texte.</summary>
    private const double MaxSafeInteger = 9_007_199_254_740_991;

    private static readonly HashSet<string> IntegerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "int", "integer", "bigint", "smallint", "tinyint", "mediumint",
    };

    private static readonly HashSet<string> NumberTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "decimal", "numeric", "money", "smallmoney", "float", "real", "double", "double precision",
    };

    private static readonly HashSet<string> BooleanTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "bit", "bool", "boolean",
    };

    private readonly Engine _engine;
    private readonly DatabaseConnectionInfo _connection;
    private readonly IQueryReaderResolver _readers;
    private readonly JavaScriptConsole _console;
    private readonly CancellationToken _cancellationToken;

    public JavaScriptDatabase(Engine engine, DatabaseConnectionInfo connection, IQueryReaderResolver readers, JavaScriptConsole console, CancellationToken cancellationToken)
    {
        _engine = engine;
        _connection = connection;
        _readers = readers;
        _console = console;
        _cancellationToken = cancellationToken;
    }

    public JsObject Create()
    {
        var db = new JsObject(_engine);
        db.CreateDataProperty("query", new ClrFunction(_engine, "query", (_, arguments) => Query(arguments), length: 2));
        return db;
    }

    private JsArray Query(JsValue[] arguments)
    {
        var sql = arguments.Length > 0 ? arguments[0] : JsValue.Undefined;
        if (!sql.IsString() || string.IsNullOrWhiteSpace(sql.AsString()))
            throw new JavaScriptException(_engine.Intrinsics.TypeError, "db.query attend une requête SQL (chaîne non vide).");

        var parameters = ToParameters(arguments.Length > 1 ? arguments[1] : JsValue.Undefined);

        // Erreur levee en JavaScript : le script peut la rattraper avec try / catch.
        if (_connection.ConnectionString is not { Length: > 0 } connectionString)
            throw new JavaScriptException(_engine.Intrinsics.Error, $"La connexion {_connection.Name} n'est pas ouverte : connectez-vous avant d'appeler db.query.");

        QueryResultSet result;
        try
        {
            var reader = _readers.Resolve(_connection.Provider);

            // Jint est synchrone : on bloque sur la lecture, l'annulation reste transmise au moteur.
            result = Task.Run(() => reader.ReadAsync(connectionString, database: null, sql.AsString(), parameters, MaxRows, _cancellationToken), _cancellationToken)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new JavaScriptException(_engine.Intrinsics.Error, ex.Message);
        }

        if (result.IsTruncated)
            _console.Write($"db.query : résultat limité aux {MaxRows:N0} premières lignes.", QueryMessageKind.Info);

        return ToArray(result);
    }

    private List<QueryParameter> ToParameters(JsValue values)
    {
        if (values.IsNull() || values.IsUndefined())
            return [];

        if (!values.IsObject() || values.IsArray())
            throw new JavaScriptException(_engine.Intrinsics.TypeError, "db.query : les paramètres sont un objet { nom: valeur }.");

        var instance = values.AsObject();
        return instance.GetOwnProperties()
            .Where(p => p.Key.IsString() && p.Value.Enumerable)
            .Select(p => ToParameter(p.Key.AsString(), instance.Get(p.Key)))
            .ToList();
    }

    private QueryParameter ToParameter(string name, JsValue value)
    {
        if (value.IsNull() || value.IsUndefined())
            return new QueryParameter(name, QueryParameterType.Text, null);

        if (value.IsString())
            return new QueryParameter(name, QueryParameterType.Text, value.AsString());

        if (value.IsBoolean())
            return new QueryParameter(name, QueryParameterType.Boolean, value.AsBoolean());

        if (value.IsDate() && !double.IsNaN(value.AsDate().DateValue))
            return new QueryParameter(name, QueryParameterType.DateTime, value.AsDate().ToDateTime());

        if (value.IsNumber() && double.IsFinite(value.AsNumber()))
        {
            var number = value.AsNumber();
            return Math.Floor(number) == number && Math.Abs(number) <= MaxSafeInteger
                ? new QueryParameter(name, QueryParameterType.Integer, (long)number)
                : new QueryParameter(name, QueryParameterType.Decimal, (decimal)number);
        }

        throw new JavaScriptException(_engine.Intrinsics.TypeError, $"db.query : paramètre {name} de type {JavaScriptValueFormatter.TypeName(value)} non pris en charge.");
    }

    private JsArray ToArray(QueryResultSet result)
    {
        var rows = new JsValue[result.Rows.Count];
        for (var r = 0; r < rows.Length; r++)
        {
            var cells = result.Rows[r].Cells;
            var row = new JsObject(_engine);
            for (var c = 0; c < result.Columns.Count && c < cells.Length; c++)
                row.CreateDataProperty(result.Columns[c].Name, ToValue(cells[c], result.Columns[c].TypeName));

            rows[r] = row;
        }

        return new JsArray(_engine, rows);
    }

    // Le lecteur rend du texte : nombres et booleens retrouvent leur type d'apres la colonne.
    private static JsValue ToValue(string? cell, string typeName)
    {
        if (cell is null)
            return JsValue.Null;

        if (BooleanTypes.Contains(typeName))
            return cell is "1" || cell.Equals("true", StringComparison.OrdinalIgnoreCase);

        var isInteger = IntegerTypes.Contains(typeName);
        if ((isInteger || NumberTypes.Contains(typeName))
            && double.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            && (!isInteger || Math.Abs(number) <= MaxSafeInteger))
            return number;

        return new JsString(cell);
    }
}
