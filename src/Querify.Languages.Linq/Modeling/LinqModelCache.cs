using Querify.Core.Ioc;
using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Schemas;
using Querify.Databases.Schemas;
using Querify.Languages.Linq.Compilation;
using System.Runtime.CompilerServices;

namespace Querify.Languages.Linq.Modeling;

/// <inheritdoc cref="ILinqModelCache"/>
[AutoRegister(As = typeof(ILinqModelCache))]
public sealed class LinqModelCache : ILinqModelCache
{
    private readonly IDatabaseSchemaCache _schemas;
    private readonly IEfModelGenerator _generator;
    private readonly ILinqCompiler _compiler;

    /// <summary>Un modele par catalogue charge : recharger le catalogue recompile le modele.</summary>
    private readonly ConditionalWeakTable<DatabaseSchemaSnapshot, Task<LinqModelAssembly>> _models = new();

    public LinqModelCache(IDatabaseSchemaCache schemas, IEfModelGenerator generator, ILinqCompiler compiler)
    {
        _schemas = schemas;
        _generator = generator;
        _compiler = compiler;
    }

    public async Task<LinqModelAssembly> GetModelAsync(DatabaseConnectionInfo connection, string? database, CancellationToken cancellationToken = default)
    {
        // La base lue donne son nom au contexte.
        var snapshot = await _schemas.GetSchemaAsync(connection, database, cancellationToken);

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

    private Task<LinqModelAssembly> Compile(DatabaseSchemaSnapshot snapshot)
        => Task.Run(() => _compiler.CompileModelAsync(_generator.Generate(snapshot)));
}
