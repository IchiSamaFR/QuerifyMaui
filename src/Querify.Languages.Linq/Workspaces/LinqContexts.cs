using Querify.Core.Ioc;
using Querify.Databases.Models.Connections;
using Querify.Databases.Schemas;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;
using Quill.Code.CSharp;
using Quill.Data.Sql;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Querify.Languages.Linq.Workspaces;

/// <inheritdoc cref="ILinqContexts"/>
[AutoRegister(As = typeof(ILinqContexts))]
public sealed class LinqContexts : ILinqContexts
{
    private readonly IDatabaseSchemaCache _schemas;
    private readonly IEfModelGenerator _generator;

    /// <summary>Un modele par catalogue charge : recharger le catalogue recompile le modele.</summary>
    private readonly ConditionalWeakTable<SqlDatabaseSchema, Task<LinqGeneratedModelAssembly>> _models = new();

    /// <summary>Un QueryGlobals par modele (libere avec lui) et par jeu de parametres (source genere, sans valeur).</summary>
    private readonly ConditionalWeakTable<LinqGeneratedModelAssembly, ConcurrentDictionary<string, Task<LinqParametersAssembly>>> _parameters = new();

    /// <summary>QueryGlobals sans modele (C# seul), par jeu de parametres.</summary>
    private readonly ConcurrentDictionary<string, Task<LinqParametersAssembly>> _parametersWithoutModel = new();

    public LinqContexts(IDatabaseSchemaCache schemas, IEfModelGenerator generator)
    {
        _schemas = schemas;
        _generator = generator;
    }

    public async Task<LinqGeneratedModelAssembly> GetModelAsync(DatabaseConnectionInfo connection, string? database, CancellationToken cancellationToken = default)
    {
        // La base lue donne son nom au contexte.
        var snapshot = await _schemas.GetSchemaAsync(connection, string.IsNullOrWhiteSpace(database) ? null : database, cancellationToken);

        var task = _models.GetValue(snapshot, Compile);
        if (task.IsFaulted)
        {
            // Un echec n'est pas garde : la prochaine demande retente la compilation.
            _models.Remove(snapshot);
            task = _models.GetValue(snapshot, Compile);
        }

        // Compilation partagee : l'annulation d'un appelant ne l'interrompt pas.
        return await task.WaitAsync(cancellationToken);
    }

    public async Task<LinqParametersAssembly> GetParametersAsync(LinqGeneratedModelAssembly? model, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        parameters ??= [];

        // Le source genere sert de cle : memes noms et types, meme assembly, quelles que soient les valeurs.
        var key = LinqQuerySource.BuildGlobals(model?.Model, parameters);
        var entries = model is null ? _parametersWithoutModel : _parameters.GetValue(model, static _ => new());

        var task = entries.GetOrAdd(key, _ => Compile(model, parameters));
        if (task.IsFaulted)
        {
            // Un echec n'est pas garde : la prochaine demande retente la compilation.
            entries.TryRemove(new KeyValuePair<string, Task<LinqParametersAssembly>>(key, task));
            task = entries.GetOrAdd(key, _ => Compile(model, parameters));
        }

        // Compilation partagee : l'annulation d'un appelant ne l'interrompt pas.
        return await task.WaitAsync(cancellationToken);
    }

    public async Task<CSharpLanguageService> GetLanguageServiceAsync(DatabaseConnectionInfo? connection, string? database, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        var model = await TryGetModelAsync(connection, database, cancellationToken);

        LinqParametersAssembly globals;
        try
        {
            globals = await GetParametersAsync(model, parameters, cancellationToken);
        }
        catch (LinqCompilationException)
        {
            // Un parametre au type inutilisable ne doit pas priver l'editeur du contexte.
            globals = await GetParametersAsync(model, [], cancellationToken);
        }

        return LinqCompiler.GetLanguageService(globals);
    }

    /// <summary>DbContext de la base, ou null : l'analyse se rabat sur le C# seul.</summary>
    private async Task<LinqGeneratedModelAssembly?> TryGetModelAsync(DatabaseConnectionInfo? connection, string? database, CancellationToken cancellationToken)
    {
        if (connection is not { IsConnected: true })
            return null;

        try
        {
            return await GetModelAsync(connection, database, cancellationToken);
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or TimeoutException or LinqCompilationException)
        {
            return null;
        }
    }

    private Task<LinqGeneratedModelAssembly> Compile(SqlDatabaseSchema snapshot)
        => Task.Run(() => LinqCompiler.CompileModelAsync(_generator.Generate(snapshot)));

    private static Task<LinqParametersAssembly> Compile(LinqGeneratedModelAssembly? model, IReadOnlyList<LinqParameter> parameters)
        => Task.Run(() => LinqCompiler.CompileParametersAsync(model, parameters));
}
