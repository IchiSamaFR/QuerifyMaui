using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Databases.Schemas;
using SqlToolManagerMaui.Services.Databases;
using System.Runtime.CompilerServices;

namespace SqlToolManagerMaui.Services.Linq;

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

    public async Task<LinqModelAssembly> GetModelAsync(DatabaseConnectionInfo connection, CancellationToken cancellationToken = default)
    {
        // Base par defaut de la connexion : c'est elle qui donne son nom au contexte.
        var snapshot = await _schemas.GetSchemaAsync(connection, null, cancellationToken);

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
        => Task.Run(() => _compiler.CompileModel(_generator.Generate(snapshot)));
}
