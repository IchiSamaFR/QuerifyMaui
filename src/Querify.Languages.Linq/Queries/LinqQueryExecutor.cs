using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Enums.Queries;
using Querify.Core.Models.Queries;
using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;
using Querify.Languages.Commons.Queries;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Compilation;
using Quill.Code.Scripting;
using System.Collections;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Querify.Languages.Linq.Queries;

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

    private readonly ILinqContexts _contexts;

    public LinqQueryExecutor(ILinqContexts contexts)
    {
        _contexts = contexts;
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
        var log = new LogOutput();
        var logIndex = 0;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var model = await _contexts.GetModelAsync(connection, database: null, cancellationToken);
            messages.Add(new QueryMessage($"Contexte {model.Model.ContextVariableName} ({model.Model.ContextClassName}) : {model.Model.EntityClassNames.Count} table(s) et vue(s)."));
            logIndex = messages.Count;

            using var compiled = await Task.Run(() => LinqCompiler.CompileQueryAsync(model, query, log: log.Write, cancellationToken: cancellationToken), cancellationToken);
            if (!compiled.Success)
            {
                hasErrors = true;
                messages.AddRange(compiled.Errors.Select(e => new QueryMessage(e.Describe(query), QueryMessageKind.Error)));
            }
            else
            {
                var outputs = await RunAsync(model, compiled, connection.ConnectionString!, MaxRows, cancellationToken);

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
        catch (CSharpRunAbandonedException)
        {
            wasCancelled = true;
            messages.Add(new QueryMessage("Le code LINQ ne peut pas être interrompu : il se termine en arrière-plan et garde sa connexion ouverte jusque-là.", QueryMessageKind.Error));
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
        }
        catch (LinqCompilationException ex)
        {
            hasErrors = true;
            messages.Add(new QueryMessage(ex.Message, QueryMessageKind.Error));
            messages.AddRange(ex.Errors.Take(20).Select(e => new QueryMessage(e, QueryMessageKind.Error)));
        }
        catch (DbException ex)
        {
            hasErrors = true;
            messages.Add(new QueryMessage(ex.Message, QueryMessageKind.Error));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Le script est du code de l'utilisateur : toute exception (traduction EF, null, division...) est un resultat.
            hasErrors = true;
            messages.Add(new QueryMessage($"{ex.GetType().Name} : {ex.Message}", QueryMessageKind.Error));
        }

        stopwatch.Stop();

        // Avant les resultats et l'eventuelle erreur : le Log a ete ecrit pendant l'execution.
        messages.InsertRange(logIndex, log.ToMessages());

        if (wasCancelled)
            messages.Add(new QueryMessage("Requête annulée par l'utilisateur.", QueryMessageKind.Error));

        messages.Add(new QueryMessage($"Heure de fin : {DateTime.Now:yyyy-MM-dd HH:mm:ss} (durée {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff})"));

        return new QueryExecutionResult(resultSets, messages, stopwatch.Elapsed, hasErrors, wasCancelled);
    }

    public async Task<QueryResultSet> ReadAsync(DatabaseConnectionInfo connection, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(parameters);

        var connectionString = connection.ConnectionString
            ?? throw new InvalidOperationException($"La connexion {connection.Name} n'est pas ouverte.");
        if (!string.IsNullOrWhiteSpace(database))
            connectionString = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = database }.ConnectionString;

        var model = await _contexts.GetModelAsync(connection, database, cancellationToken);
        var variables = parameters.Select(LinqParameter.From).ToArray();

        using var compiled = await Task.Run(() => LinqCompiler.CompileQueryAsync(model, query, variables, cancellationToken: cancellationToken), cancellationToken);
        if (!compiled.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, compiled.Errors.Take(5).Select(e => e.Describe(query))));
        }

        var outputs = await RunAsync(model, compiled, connectionString, Math.Max(1, maxRows), cancellationToken);

        // Une carte n'affiche qu'un jeu : la valeur rendue, a defaut le dernier Dump().
        return outputs.Count > 0
            ? outputs[^1].ResultSet
            : throw new InvalidOperationException("Aucun résultat : terminez la requête par une expression, un return ou .Dump().");
    }

    // ---------- Execution ----------

    private sealed record Output(QueryResultSet ResultSet, string? Sql);

    /// <summary>
    /// Execution par Quill : l'annulation rend la main tout de suite, mais le code de l'utilisateur ne peut pas etre
    /// interrompu dans le processus (boucle infinie...) ; il se termine en arriere-plan
    /// (<see cref="CSharpRunAbandonedException"/>) et son assembly n'est dechargee qu'a sa fin reelle.
    /// </summary>
    private Task<List<Output>> RunAsync(LinqModelAssembly model, LinqCompiledQuery compiled, string connectionString, int maxRows, CancellationToken cancellationToken)
        => compiled.Compilation!.RunAsync(_ => Run(model, compiled, connectionString, maxRows, cancellationToken), cancellationToken);

    private List<Output> Run(LinqModelAssembly model, LinqCompiledQuery compiled, string connectionString, int maxRows, CancellationToken cancellationToken)
    {
        using var context = LinqCompiler.CreateContext(model, connectionString);

        List<Output> outputs = [];
        object? lastDumped = null;

        // Capture immediate : un Dump() montre la valeur a cet instant, meme si le script la modifie ensuite.
        var value = compiled.Execute!(context, (dumped, title) =>
        {
            lastDumped = dumped;
            outputs.Add(Capture(dumped, outputs.Count + 1, title, maxRows, cancellationToken));
        });

        // return x.Dump() : la valeur est deja affichee, pas de second onglet identique.
        var isAlreadyDumped = outputs.Count > 0 && Equals(value, lastDumped);
        if (compiled.ReturnsValue && !isAlreadyDumped)
            outputs.Add(Capture(value, outputs.Count + 1, title: null, maxRows, cancellationToken));

        return outputs;
    }

    /// <summary>Sortie de Log.* ; le script peut encore ecrire en arriere-plan apres une annulation.</summary>
    private sealed class LogOutput
    {
        // Une boucle d'affichage ne doit pas saturer la memoire ni l'onglet Messages (un seul bloc de texte).
        private const int MaxLength = 200_000;

        private readonly Lock _lock = new();
        private readonly List<QueryMessage> _messages = [];
        private int _length;
        private bool _isTruncated;

        public void Write(LogLevel level, string text)
        {
            lock (_lock)
            {
                if (_isTruncated)
                    return;

                // Le saut de ligne compte : une boucle de messages vides reste bornee.
                var room = MaxLength - _length - 1;
                if (text.Length > room)
                {
                    _isTruncated = true;
                    text = text[..Math.Max(0, room)];
                }

                _length += text.Length + 1;
                _messages.Add(new QueryMessage(text.ReplaceLineEndings(), QueryMessageKind.Output, level));
            }
        }

        public IReadOnlyList<QueryMessage> ToMessages()
        {
            lock (_lock)
            {
                return _isTruncated
                    ? [.. _messages, new QueryMessage($"Sortie Log tronquée à {MaxLength:N0} caractères.", QueryMessageKind.Error)]
                    : [.. _messages];
            }
        }
    }

    private static Output Capture(object? value, int index, string? title, int maxRows, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? sql = null;
        if (value is IQueryable queryable)
        {
            sql = TryGetSql(queryable);

            // Plafond applique cote serveur (TOP), une ligne de plus pour savoir s'il a ete atteint.
            value = QueryableTake.MakeGenericMethod(queryable.ElementType).Invoke(null, [queryable, maxRows + 1]);
        }

        return new Output(ToResultSet(value, index, title, maxRows, cancellationToken), sql);
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

    private static QueryResultSet ToResultSet(object? value, int index, string? title, int maxRows, CancellationToken cancellationToken)
    {
        // Une valeur seule (Count(), First()...) s'affiche comme une ligne d'une colonne.
        if (value is null or string || value is not IEnumerable items || IsScalar(value.GetType()))
            return Build(ElementTypeOf(value?.GetType()), [value], isTruncated: false, index, title);

        List<object?> rows = [];
        var isTruncated = false;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (rows.Count == maxRows)
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
        if (items.Any(i => i is not null) && items.All(i => i is null || AsPairs(i) is not null))
            return BuildFromDictionaries(items, isTruncated, index, title);

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

    // Colonnes dynamiques : union ordonnee des cles de toutes les lignes (une cle absente = cellule vide).
    private static QueryResultSet BuildFromDictionaries(IReadOnlyList<object?> items, bool isTruncated, int index, string? title)
    {
        var data = items.Select(i => (i is null ? Enumerable.Empty<KeyValuePair<string, object?>>() : AsPairs(i)!).ToList()).ToList();

        var names = data.SelectMany(d => d.Select(p => p.Key)).Distinct().ToList();
        var columns = names
            .Select(n => new QueryResultColumn(n, FriendlyName(data.SelectMany(d => d).FirstOrDefault(p => p.Key == n && p.Value is not null).Value?.GetType() ?? typeof(object))))
            .ToArray();

        var rows = data
            .Select((d, i) =>
            {
                var values = d.GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.Last().Value);
                return new QueryResultRow(i + 1, names.Select(n => values.TryGetValue(n, out var v) ? Format(v) : null).ToArray());
            })
            .ToArray();

        return new QueryResultSet(index, columns, rows, isTruncated, title);
    }

    private static IEnumerable<KeyValuePair<string, object?>>? AsPairs(object item) => item switch
    {
        IDictionary<string, object?> d => d,
        IDictionary d => d.Cast<DictionaryEntry>().Select(e => new KeyValuePair<string, object?>(Convert.ToString(e.Key, CultureInfo.InvariantCulture) ?? string.Empty, e.Value)),
        _ => null,
    };

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
            // Un type imbrique dans un generique (Dictionary<,>.KeyCollection) n'a pas de backtick.
            var tick = type.Name.IndexOf('`', StringComparison.Ordinal);
            var name = tick >= 0 ? type.Name[..tick] : type.Name;
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
