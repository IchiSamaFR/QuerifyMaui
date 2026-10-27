using Microsoft.CodeAnalysis.CSharp;
using Querify.Core.Ioc;
using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;
using Quill.Code.CSharp.Workspaces;
using System.Data.Common;

namespace Querify.Languages.Linq.Workspaces;

/// <inheritdoc cref="ILinqEditorDocuments"/>
[AutoRegister(As = typeof(ILinqEditorDocuments))]
public sealed class LinqEditorDocuments : ILinqEditorDocuments
{
    private readonly ILinqModelCache _models;
    private readonly ILinqWorkspace _workspace;

    public LinqEditorDocuments(ILinqModelCache models, ILinqWorkspace workspace)
    {
        _models = models;
        _workspace = workspace;
    }

    public async Task<CSharpDocument> GetAsync(string query, DatabaseConnectionInfo? connection, string? database, IReadOnlyList<QueryParameter> parameters, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(parameters);

        var model = await TryGetModelAsync(connection, database, cancellationToken);
        return _workspace.GetDocument(model, query, ToParameters(parameters));
    }

    /// <summary>DbContext de la base, ou null (pas de connexion, base injoignable, modele invalide) : l'analyse se rabat sur le C# seul.</summary>
    private async Task<LinqModelAssembly?> TryGetModelAsync(DatabaseConnectionInfo? connection, string? database, CancellationToken cancellationToken)
    {
        if (connection is not { IsConnected: true })
            return null;

        try
        {
            return await _models.GetModelAsync(connection, string.IsNullOrWhiteSpace(database) ? null : database, cancellationToken);
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or TimeoutException or LinqCompilationException)
        {
            return null;
        }
    }

    /// <summary>Parametres du dashboard en variables C# ; un nom invalide en C# est ignore, l'execution le signalera.</summary>
    private static LinqParameter[] ToParameters(IReadOnlyList<QueryParameter> parameters)
        => parameters
            .Where(p => SyntaxFacts.IsValidIdentifier(p.Name))
            .Select(LinqParameter.From)
            .ToArray();
}
