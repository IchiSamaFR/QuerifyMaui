using Querify.Core.Ioc;
using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Schemas;
using Querify.Databases.Schemas;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;
using Quill.Code.CSharp;
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
    private readonly ConditionalWeakTable<DatabaseSchemaSnapshot, Task<LinqModelAssembly>> _models = new();

    public LinqContexts(IDatabaseSchemaCache schemas, IEfModelGenerator generator)
    {
        _schemas = schemas;
        _generator = generator;
    }

    public async Task<LinqModelAssembly> GetModelAsync(DatabaseConnectionInfo connection, string? database, CancellationToken cancellationToken = default)
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

    public async Task<CSharpLanguageService> GetLanguageServiceAsync(DatabaseConnectionInfo? connection, string? database, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
        => LinqCompiler.GetLanguageService(await TryGetModelAsync(connection, database, cancellationToken), parameters);

    /// <summary>DbContext de la base, ou null : l'analyse se rabat sur le C# seul.</summary>
    private async Task<LinqModelAssembly?> TryGetModelAsync(DatabaseConnectionInfo? connection, string? database, CancellationToken cancellationToken)
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

    private Task<LinqModelAssembly> Compile(DatabaseSchemaSnapshot snapshot)
        => Task.Run(() => LinqCompiler.CompileModelAsync(_generator.Generate(snapshot)));
}
