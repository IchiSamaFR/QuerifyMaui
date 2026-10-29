using Querify.Core.Enums.Editors.Highlighting;
using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;
using Quill.Code;

namespace Querify.Languages.Commons.Editors;

/// <summary>Services d'editeur (Quill) d'un langage ; une implementation par langage.</summary>
public interface IEditorLanguage
{
    SyntaxLanguage Language { get; }

    /// <summary>
    /// Service d'un editeur, lie a son contexte. L'etat de la connexion est relu a chaque demande :
    /// une connexion qui s'ouvre enrichit les reponses sans recreer le service.
    /// </summary>
    /// <param name="database">Base interrogee par le texte ; null = base de la connexion.</param>
    /// <param name="parameters">Parametres fournis a l'execution (dashboard), connus avant d'etre ecrits.</param>
    ICodeLanguageService CreateService(DatabaseConnectionInfo? connection, string? database, IReadOnlyList<QueryParameter> parameters);

    /// <summary>Precharge ce dont la premiere demande aura besoin (catalogue, Roslyn...) ; n'echoue jamais.</summary>
    Task WarmUpAsync(DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default);
}
