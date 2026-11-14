using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Databases.Models.Connections;
using Querify.Databases.Schemas;
using Querify.Languages.Editors;
using Querify.Languages.Queries;
using Quill.Code;
using Quill.Code.CSharp.Linq;
using Quill.Code.Execution;
using Quill.Data.Sql;
using System.Globalization;

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

    /// <summary>
    /// Instruction sur le contexte (Db) qui affiche les premieres lignes, entite nommee d'apres la table comme le DbContext genere.
    /// Une table homonyme dans un autre schema y est prefixee du schema : le nom est alors a ajuster.
    /// </summary>
    public string CreateSelectFirstRows(InsertQueryMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return string.Create(CultureInfo.InvariantCulture, $"Db.{message.Table}.Take({message.RowCount}).Dump();");
    }

    /// <summary>Structure relue a chaque demande : une connexion qui s'ouvre donne le DbContext sans recreer le service.</summary>
    private LinqLanguageOptions Options(DatabaseConnectionInfo? connection, string? database, IReadOnlyList<QueryParameter> parameters) => new()
    {
        Provider = connection is null ? null : LinqDatabaseProviders.Find(connection),
        Schema = connection is null ? null : cancellationToken => GetSchemaAsync(connection, database, cancellationToken),
        Parameters = parameters,
    };

    private async Task<SqlDatabaseSchema?> GetSchemaAsync(DatabaseConnectionInfo connection, string? database, CancellationToken cancellationToken)
        => connection.IsConnected
            ? await _schemas.GetSchemaAsync(connection, string.IsNullOrWhiteSpace(database) ? null : database, cancellationToken)
            : null;
}
