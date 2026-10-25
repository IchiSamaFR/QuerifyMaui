using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Databases.Models.Connections;

namespace SqlToolManagerMaui.Languages.JavaScript;

/// <summary>Moteur commun des executeurs JavaScript et TypeScript (Jint, sans acces CLR).</summary>
public interface IJavaScriptRunner
{
    /// <summary>
    /// Execute le script sur la connexion ; <paramref name="transform"/> le convertit en JavaScript avant
    /// l'analyse (retrait des types TypeScript). Les erreurs du script sont rapportees dans les messages.
    /// </summary>
    Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string script, Func<string, string>? transform, CancellationToken cancellationToken = default);
}
