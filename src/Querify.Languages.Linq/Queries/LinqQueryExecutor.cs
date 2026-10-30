using Microsoft.Data.SqlClient;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Databases.Models.Connections;
using Querify.Languages.Commons.Queries;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Compilation;
using Quill.Code.Scripting;
using System.Data.Common;
using System.Diagnostics;
using Quill.Code.Execution;
using Quill.Code.SqlServer.Execution;
using Quill.Code.SqlServer.Constants;

namespace Querify.Languages.Linq.Queries;

/// <summary>Execution d'une requete LINQ sur le DbContext genere a partir du catalogue de la connexion.</summary>
[AutoRegister(As = typeof(IQueryExecutor))]
public sealed class LinqQueryExecutor : IQueryExecutor
{
    private const int MaxRows = QueryExecutionLimits.MaxRowsPerResultSet;

    private readonly ILinqContexts _contexts;

    public LinqQueryExecutor(ILinqContexts contexts)
    {
        _contexts = contexts;
    }

    public SyntaxLanguage Language => SyntaxLanguage.Linq;

    public async Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string query, IProgress<QueryMessage>? liveMessages = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(query);

        List<QueryResultSet> resultSets = [];
        List<QueryMessage> messages = [];
        var hasErrors = false;
        var wasCancelled = false;
        var log = new LogOutput(liveMessages);
        var logIndex = 0;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var model = await _contexts.GetModelAsync(connection, database: null, cancellationToken);
            messages.Add(new QueryMessage($"Contexte {model.Model.ContextClassName} (QueryDbContext) : {model.Model.EntityClassNames.Count} table(s) et vue(s)."));
            logIndex = messages.Count;

            var globals = await _contexts.GetParametersAsync(model, cancellationToken: cancellationToken);
            // SQL genere et Log.* par Log (au fil de l'eau), jeux de resultats par Data.
            var options = new DataRunOptions { Log = log.Write, Data = resultSets.Add };
            await RunAsync(globals, query, connection.ConnectionString!, options, MaxRows, values: null, cancellationToken);

            foreach (var resultSet in resultSets)
            {
                // Plusieurs jeux : chaque message dit a quel onglet il se rapporte.
                var prefix = resultSets.Count > 1 ? $"{resultSet.Title} : " : string.Empty;
                messages.Add(new QueryMessage($"{prefix}({resultSet.Rows.Count} ligne(s))"));
            }

            if (resultSets.Count == 0)
                messages.Add(new QueryMessage("Aucun résultat : affichez les données avec .Dump()."));
        }
        catch (CSharpCompilationException ex)
        {
            hasErrors = true;
            messages.AddRange(ex.Errors.Select(e => new QueryMessage(e.Describe(query), QueryMessageKind.Error)));
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
        var globals = await _contexts.GetParametersAsync(model, variables, cancellationToken);

        List<QueryResultSet> resultSets = [];
        try
        {
            await RunAsync(globals, query, connectionString, new DataRunOptions { Data = resultSets.Add }, Math.Max(1, maxRows), variables, cancellationToken);
        }
        catch (CSharpCompilationException ex)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, ex.Errors.Take(5).Select(e => e.Describe(query))), ex);
        }

        // Une carte n'affiche qu'un jeu : le dernier Dump().
        return resultSets.Count > 0
            ? resultSets[^1]
            : throw new InvalidOperationException("Aucun résultat : le script doit afficher les données avec .Dump().");
    }

    // ---------- Execution ----------

    /// <summary>
    /// Execution par Quill : l'annulation rend la main tout de suite, mais le code de l'utilisateur ne peut pas etre
    /// interrompu dans le processus (boucle infinie...) ; il se termine en arriere-plan
    /// (<see cref="CSharpRunAbandonedException"/>) et son assembly n'est dechargee qu'a sa fin reelle.
    /// </summary>
    /// <param name="values">Valeurs des parametres du dashboard, posees dans QueryGlobals ; null : aucun.</param>
    private static async Task RunAsync(LinqParametersAssembly globals, string query, string connectionString, DataRunOptions options, int maxRows, IReadOnlyList<LinqParameter>? values, CancellationToken cancellationToken)
    {
        var context = LinqCompiler.CreateContext(globals.Model!, connectionString);
        var isAbandoned = false;

        try
        {
            await LinqCompiler.RunAsync(globals, query, context, options, maxRows, values, cancellationToken);
        }
        catch (CSharpRunAbandonedException)
        {
            // Le script tourne encore sur ce contexte : il le garde (et sa connexion) jusqu'a sa fin.
            isAbandoned = true;
            throw;
        }
        finally
        {
            if (!isAbandoned)
                context.Dispose();
        }
    }

    /// <summary>Sortie de Log.* ; le script peut encore ecrire en arriere-plan apres une annulation.</summary>
    private sealed class LogOutput
    {
        // Une boucle d'affichage ne doit pas saturer la memoire ni l'onglet Messages (un seul bloc de texte).
        private const int MaxLength = 200_000;

        private readonly Lock _lock = new();
        private readonly List<QueryMessage> _messages = [];
        private readonly IProgress<QueryMessage>? _live;
        private int _length;
        private bool _isTruncated;

        public LogOutput(IProgress<QueryMessage>? live)
        {
            _live = live;
        }

        public void Write(LogLevel level, string text)
        {
            QueryMessage message;
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
                message = new QueryMessage(text.ReplaceLineEndings(), QueryMessageKind.Output, level);
                _messages.Add(message);
            }

            // Hors verrou : le rapporteur ne doit pas bloquer les autres ecritures du script.
            _live?.Report(message);
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
}
