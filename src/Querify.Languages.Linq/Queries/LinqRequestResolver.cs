using Microsoft.CodeAnalysis.CSharp;
using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Modeling;
using System.Data.Common;

namespace Querify.Languages.Linq.Queries;

/// <summary>Ce que l'autocompletion, le survol, les signatures et les erreurs tirent d'une demande de l'editeur.</summary>
internal static class LinqRequestResolver
{
    /// <summary>DbContext de la base, ou null (pas de connexion, base injoignable, modele invalide) : l'analyse se rabat sur le C# seul.</summary>
    public static async Task<LinqModelAssembly?> TryGetModelAsync(ILinqModelCache models, DatabaseConnectionInfo? connection, string? database, CancellationToken cancellationToken)
    {
        if (connection is not { IsConnected: true })
            return null;

        try
        {
            return await models.GetModelAsync(connection, string.IsNullOrWhiteSpace(database) ? null : database, cancellationToken);
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or TimeoutException or LinqCompilationException)
        {
            return null;
        }
    }

    /// <summary>Parametres du dashboard en variables C# ; un nom invalide en C# est ignore, l'execution le signalera.</summary>
    public static LinqParameter[] ToParameters(IReadOnlyList<QueryParameter> parameters)
        => parameters
            .Where(p => SyntaxFacts.IsValidIdentifier(p.Name))
            .Select(LinqParameter.From)
            .ToArray();
}
