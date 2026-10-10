using Microsoft.EntityFrameworkCore;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Languages.Commons.Queries;
using System.Collections;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace SqlToolManagerMaui.Languages.Linq;

/// <summary>Execution d'une requete LINQ sur le DbContext genere a partir du catalogue de la connexion.</summary>
[AutoRegister(As = typeof(IQueryExecutor))]
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "App non publiee en trimmed : reflexion sur les resultats assumee.")]
[UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "App non publiee en trimmed : reflexion sur les resultats assumee.")]
[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "App non publiee en trimmed : reflexion sur les resultats assumee.")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "App non publiee en trimmed : reflexion sur les resultats assumee.")]
[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "App non publiee en AOT : reflexion sur les resultats assumee.")]
public sealed class LinqQueryExecutor : IQueryExecutor
{
    private const int MaxRows = QueryExecutionLimits.MaxRowsPerResultSet;

    private static readonly MethodInfo QueryableTake = typeof(Queryable).GetMethods()
        .Single(m => m.Name == nameof(Queryable.Take) && m.GetParameters()[1].ParameterType == typeof(int));

    private readonly ILinqModelCache _models;
    private readonly ILinqCompiler _compiler;

    public LinqQueryExecutor(ILinqModelCache models, ILinqCompiler compiler)
    {
        _models = models;
        _compiler = compiler;
    }

    public SyntaxLanguage Language => SyntaxLanguage.Linq;

    public async Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(query);

        List<QueryResultSet> resultSets = [];
        List<QueryMessage> messages = [];
        var hasErrors = false;
        var wasCancelled = false;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var model = await _models.GetModelAsync(connection, cancellationToken);
            messages.Add(new QueryMessage($"Contexte {model.Model.ContextVariableName} ({model.Model.ContextClassName}) : {model.Model.EntityClassNames.Count} table(s) et vue(s)."));

            var compiled = await Task.Run(() => _compiler.CompileQuery(model, query), cancellationToken);
            if (!compiled.Success)
            {
                hasErrors = true;
                messages.AddRange(compiled.Errors.Select(e => new QueryMessage(e.ToString(), IsError: true)));
            }
            else
            {
                var outputs = await Task.Run(() => Run(model, compiled, connection.ConnectionString!, cancellationToken), cancellationToken);

                foreach (var output in outputs)
                {
                    // Plusieurs jeux : chaque message dit a quel onglet il se rapporte.
                    var prefix = outputs.Count > 1 ? $"{output.ResultSet.Title} : " : string.Empty;

                    if (output.Sql is not null)
                        messages.Add(new QueryMessage($"{prefix}SQL généré :{Environment.NewLine}{output.Sql}"));

                    resultSets.Add(output.ResultSet);
                    messages.Add(new QueryMessage($"{prefix}({output.ResultSet.Rows.Count} ligne(s))"));
                }

                if (outputs.Count == 0)
                    messages.Add(new QueryMessage("Aucun résultat : terminez par return ou affichez une valeur avec .Dump()."));
            }
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
        }
        catch (LinqCompilationException ex)
        {
            hasErrors = true;
            messages.Add(new QueryMessage(ex.Message, IsError: true));
            messages.AddRange(ex.Errors.Take(20).Select(e => new QueryMessage(e.ToString(), IsError: true)));
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or ArgumentException or NotSupportedException or InvalidCastException or NullReferenceException or TimeoutException)
        {
            // Traduction EF impossible, erreur SQL, null dans la requete... : rapporte comme une erreur SQL.
            hasErrors = true;
            messages.Add(new QueryMessage(ex.Message, IsError: true));
        }

        stopwatch.Stop();

        if (wasCancelled)
            messages.Add(new QueryMessage("Requête annulée par l'utilisateur.", IsError: true));

        messages.Add(new QueryMessage($"Heure de fin : {DateTime.Now:yyyy-MM-dd HH:mm:ss} (durée {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff})"));

        return new QueryExecutionResult(resultSets, messages, stopwatch.Elapsed, hasErrors, wasCancelled);
    }

    // ---------- Execution ----------

    private sealed record Output(QueryResultSet ResultSet, string? Sql);

    private List<Output> Run(LinqModelAssembly model, LinqCompiledQuery compiled, string connectionString, CancellationToken cancellationToken)
    {
        using var context = _compiler.CreateContext(model, connectionString);

        List<Output> outputs = [];
        object? lastDumped = null;

        // Capture immediate : un Dump() montre la valeur a cet instant, meme si le script la modifie ensuite.
        var value = compiled.Execute!(context, (dumped, title) =>
        {
            lastDumped = dumped;
            outputs.Add(Capture(dumped, outputs.Count + 1, title, cancellationToken));
        });

        // return x.Dump() : la valeur est deja affichee, pas de second onglet identique.
        var isAlreadyDumped = outputs.Count > 0 && Equals(value, lastDumped);
        if (compiled.ReturnsValue && !isAlreadyDumped)
            outputs.Add(Capture(value, outputs.Count + 1, title: null, cancellationToken));

        return outputs;
    }

    private static Output Capture(object? value, int index, string? title, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? sql = null;
        if (value is IQueryable queryable)
        {
            sql = TryGetSql(queryable);

            // Plafond applique cote serveur (TOP), une ligne de plus pour savoir s'il a ete atteint.
            value = QueryableTake.MakeGenericMethod(queryable.ElementType).Invoke(null, [queryable, MaxRows + 1]);
        }

        return new Output(ToResultSet(value, index, title, cancellationToken), sql);
    }

    private static string? TryGetSql(IQueryable queryable)
    {
        try
        {
            return queryable.ToQueryString();
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            // Requete evaluee cote client (LINQ to Objects) : pas de SQL a montrer.
            return null;
        }
    }

    // ---------- Conversion en grille ----------

    private static QueryResultSet ToResultSet(object? value, int index, string? title, CancellationToken cancellationToken)
    {
        // Une valeur seule (Count(), First()...) s'affiche comme une ligne d'une colonne.
        if (value is null or string || value is not IEnumerable items || IsScalar(value.GetType()))
            return Build(ElementTypeOf(value?.GetType()), [value], isTruncated: false, index, title);

        List<object?> rows = [];
        var isTruncated = false;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (rows.Count == MaxRows)
            {
                isTruncated = true;
                break;
            }

            rows.Add(item);
        }

        var elementType = EnumerableElementType(value.GetType()) is { } declared && declared != typeof(object)
            ? declared
            : rows.FirstOrDefault(r => r is not null)?.GetType() ?? typeof(object);

        return Build(elementType, rows, isTruncated, index, title);
    }

    private static QueryResultSet Build(Type elementType, IReadOnlyList<object?> items, bool isTruncated, int index, string? title)
    {
        if (IsScalar(elementType))
        {
            var column = new QueryResultColumn("Valeur", FriendlyName(elementType));
            var scalarRows = items.Select((item, i) => new QueryResultRow(i + 1, [Format(item)])).ToArray();
            return new QueryResultSet(index, [column], scalarRows, isTruncated, title);
        }

        var properties = elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();

        var columns = properties.Select(p => new QueryResultColumn(p.Name, FriendlyName(p.PropertyType))).ToArray();
        var rows = items.Select((item, i) => new QueryResultRow(i + 1, properties.Select(p => item is null ? null : Format(p.GetValue(item))).ToArray())).ToArray();

        return new QueryResultSet(index, columns, rows, isTruncated, title);
    }

    private static Type ElementTypeOf(Type? type) => type ?? typeof(object);

    private static Type? EnumerableElementType(Type type)
        => type.GetInterfaces().Append(type)
               .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
               ?.GetGenericArguments()[0];

    private static bool IsScalar(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(object)
            || underlying == typeof(string) || underlying == typeof(decimal) || underlying == typeof(byte[])
            || underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset) || underlying == typeof(DateOnly)
            || underlying == typeof(TimeOnly) || underlying == typeof(TimeSpan) || underlying == typeof(Guid);
    }

    /// <summary>Nom C# court : int?, string, List&lt;Commande&gt;...</summary>
    private static string FriendlyName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return FriendlyName(underlying) + "?";

        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FriendlyName))}>";
        }

        return type.Name switch
        {
            "Int32" => "int",
            "Int64" => "long",
            "Int16" => "short",
            "Byte" => "byte",
            "Boolean" => "bool",
            "String" => "string",
            "Decimal" => "decimal",
            "Double" => "double",
            "Single" => "float",
            "Object" => "object",
            "Byte[]" => "byte[]",
            _ => type.Name
        };
    }

    /// <summary>Memes conventions d'affichage que l'execution SQL.</summary>
    private static string? Format(object? value) => value switch
    {
        null => null,
        string s => s,
        bool b => b ? "1" : "0",
        DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString("yyyy-MM-dd HH:mm:ss.fffffff zzz", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        byte[] bytes => "0x" + Convert.ToHexString(bytes, 0, Math.Min(bytes.Length, 64)) + (bytes.Length > 64 ? "…" : string.Empty),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        // Collection imbriquee (Select(u => u.Roles)...) : on en donne la taille plutot que le nom du type.
        IEnumerable e => $"({e.Cast<object?>().Count()} élément(s))",
        _ => value.ToString(),
    };
}
