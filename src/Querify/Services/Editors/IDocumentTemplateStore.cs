using Querify.Core.Enums.Editors.Highlighting;
using Querify.Databases.Enums;

namespace Querify.Services.Editors;

/// <summary>
/// Modeles inseres dans les nouveaux scripts : Resources/Raw/Templates/&lt;langage&gt;&lt;extension&gt;, ou
/// &lt;langage&gt;.&lt;moteur&gt;&lt;extension&gt; quand le moteur a le sien (Sql.Oracle.sql) ; SQL Server sans connexion.
/// </summary>
public interface IDocumentTemplateStore
{
    /// <summary>Texte du modele du langage, ecrit pour le moteur de la connexion courante s'il a le sien ; vide sans modele.</summary>
    Task<string> GetAsync(SyntaxLanguage language, DatabaseProvider? provider, CancellationToken cancellationToken = default);
}
