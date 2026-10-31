using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Databases.Models.Connections;
using Querify.Databases.Schemas;
using Querify.Languages.Editors;
using Quill.Code;
using Quill.Code.CSharp.Linq;
using Quill.Code.Execution;
using Quill.Data.Sql;

namespace Querify.Languages.Linq.Editors;

/// <summary>
/// Editeur des onglets LINQ : <see cref="LinqLanguageService"/> de Quill sur la structure de la base lue par le cache
/// des schemas (meme instance, meme DbContext compile que l'execution), C# seul tant que la connexion est fermee.
/// </summary>
[AutoRegister(As = typeof(IEditorLanguage))]
public sealed class LinqEditorLanguage : IEditorLanguage
{
    private readonly IDatabaseSchemaCache _schemas;

    public LinqEditorLanguage(IDatabaseSchemaCache schemas)
    {
        _schemas = schemas;
    }

    public SyntaxLanguage Language => SyntaxLanguage.Linq;

    public ICodeLanguageService CreateService(DatabaseConnectionInfo? connection, string? database, IReadOnlyList<QueryParameter> parameters)
        => new LinqLanguageService(Options(connection, database, parameters));

    /// <summary>Roslyn et DbContext genere prepares a l'ouverture de l'onglet ; n'echoue jamais.</summary>
    public Task WarmUpAsync(DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
        => new LinqLanguageService(Options(connection, database: null, [])).WarmUpAsync(cancellationToken);

    /// <summary>Structure relue a chaque demande : une connexion qui s'ouvre donne le DbContext sans recreer le service.</summary>
    private LinqLanguageOptions Options(DatabaseConnectionInfo? connection, string? database, IReadOnlyList<QueryParameter> parameters) => new()
    {
        Schema = connection is null ? null : cancellationToken => GetSchemaAsync(connection, database, cancellationToken),
        Parameters = parameters,
    };

    private async Task<SqlDatabaseSchema?> GetSchemaAsync(DatabaseConnectionInfo connection, string? database, CancellationToken cancellationToken)
        => connection.IsConnected
            ? await _schemas.GetSchemaAsync(connection, string.IsNullOrWhiteSpace(database) ? null : database, cancellationToken)
            : null;
}
