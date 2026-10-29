using Querify.Databases.Enums;
using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;
using Querify.Databases.Models.Schemas;
using Querify.Databases.Schemas;
using Quill.Code.Analysis;
using Quill.Code;
using System.Data.Common;
using System.Text;
using Quill.Code.Sql;
using Quill.Code.Sql.Lexing;
using Quill.Code.Sql.Completion;

namespace Querify.Languages.Sql.Analysis;

/// <summary>
/// Infobulle d'un onglet SQL : type d'une colonne, colonnes d'une table ou d'un alias, type d'une variable
/// declaree ou d'un parametre du dashboard. Le schema vient du catalogue deja charge pour l'autocompletion.
/// </summary>
internal sealed class SqlSchemaHover : ICodeHover
{
    /// <summary>Colonnes listees dans l'infobulle d'une table : au-dela, elle deborderait de l'editeur.</summary>
    private const int MaxListedColumns = 12;

    /// <summary>Mots qui terminent une instruction DECLARE.</summary>
    private static readonly HashSet<string> StatementWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "SET", "DECLARE", "INSERT", "UPDATE", "DELETE", "MERGE", "IF", "WHILE", "BEGIN", "END",
        "EXEC", "EXECUTE", "RETURN", "PRINT", "WITH", "GO", "USE"
    };

    private readonly IDatabaseSchemaCache _cache;
    private readonly DatabaseConnectionInfo? _connection;
    private readonly string? _database;
    private readonly IReadOnlyList<QueryParameter> _parameters;

    public SqlSchemaHover(IDatabaseSchemaCache cache, DatabaseConnectionInfo? connection, string? database, IReadOnlyList<QueryParameter> parameters)
    {
        _cache = cache;
        _connection = connection;
        _database = database;
        _parameters = parameters;
    }

    public async Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        // Commentaires ecartes : rien a expliquer dedans, et ils casseraient le qualificatif (a./*x*/b).
        var text = code;
        var tokens = SqlLexer.Tokenize(text).Where(t => !t.IsComment).ToArray();
        // Le caractere survole, ou celui qui le precede (souris posee juste apres la fin du nom).
        var index = Array.FindIndex(tokens, t => t.Start <= position && position < t.End);
        if (index < 0)
            index = Array.FindIndex(tokens, t => t.Start < position && position == t.End);
        if (index < 0)
            return null;

        var token = tokens[index];
        if (token.Kind == SqlTokenKind.Variable)
            return DescribeVariable(text, tokens, token, _parameters);

        if (token.Kind is not (SqlTokenKind.Word or SqlTokenKind.QuotedIdentifier) || _connection is not { IsConnected: true } connection)
            return null;

        try
        {
            var scope = SqlContextAnalyzer.AnalyzeScope(text, token.Start, _database);
            var name = SqlContextAnalyzer.Unquote(token.GetText(text));
            var described = await DescribeNameAsync(connection, scope, ReadQualifier(text, tokens, index), name, cancellationToken);

            return described is { } hover ? new HoverInfo(token.Start, token.Length, hover.Signature, hover.Description) : null;
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or TimeoutException)
        {
            // Serveur injoignable ou droits insuffisants : pas d'infobulle plutot qu'une erreur au survol.
            return null;
        }
    }

    /// <summary>Parties avant le nom : ["u"] pour u.Name, ["dbo", "Users"] pour dbo.Users.Name.</summary>
    private static List<string> ReadQualifier(string text, SqlToken[] tokens, int index)
    {
        List<string> parts = [];
        for (var i = index - 1; i >= 1 && tokens[i].Kind == SqlTokenKind.Dot && tokens[i - 1].Kind is SqlTokenKind.Word or SqlTokenKind.QuotedIdentifier; i -= 2)
            parts.Insert(0, SqlContextAnalyzer.Unquote(tokens[i - 1].GetText(text)));

        return parts;
    }

    // ---------- Noms du schema ----------

    private async Task<(string Signature, string? Description)?> DescribeNameAsync(DatabaseConnectionInfo connection, SqlContext scope,
        List<string> qualifier, string name, CancellationToken cancellationToken)
    {
        switch (qualifier.Count)
        {
            case 0:
                {
                    // Alias ou table du FROM, puis colonne d'une table du lot, puis objet, schema ou base.
                    if (scope.FindTable(name) is { } reference && await FindAsync(connection, scope, reference, cancellationToken) is { } referenced)
                        return DescribeObject(referenced, reference.Alias);

                    List<(DatabaseSchemaObject Table, DatabaseSchemaColumn Column)> matches = [];
                    foreach (var table in scope.Tables)
                    {
                        if (await FindAsync(connection, scope, table, cancellationToken) is { } source
                            && FindColumn(source, name) is { } column
                            && !matches.Any(m => ReferenceEquals(m.Table, source)))
                            matches.Add((source, column));
                    }

                    if (matches.Count > 0)
                        return DescribeColumn(matches[0].Table, matches[0].Column, matches.Skip(1).Select(m => QualifiedName(m.Table)).ToArray());

                    var snapshot = await _cache.GetSchemaAsync(connection, scope.Database, cancellationToken);
                    if (snapshot.Find(null, name) is { } direct)
                        return DescribeObject(direct, null);

                    if (snapshot.HasSchema(name))
                        return ($"(schéma) {name}", $"Base {snapshot.Database}");

                    return await IsDatabaseAsync(connection, name, cancellationToken) ? ($"(base de données) {name}", null) : null;
                }

            case 1:
                {
                    var owner = qualifier[0];
                    if (scope.FindTable(owner) is { } reference && await FindAsync(connection, scope, reference, cancellationToken) is { } aliased)
                        return ColumnOf(aliased, name);

                    var snapshot = await _cache.GetSchemaAsync(connection, scope.Database, cancellationToken);
                    if (snapshot.HasSchema(owner) && snapshot.Find(owner, name) is { } inSchema)
                        return DescribeObject(inSchema, null);

                    if (snapshot.Find(null, owner) is { } table)
                        return ColumnOf(table, name);

                    if (await IsDatabaseAsync(connection, owner, cancellationToken) && (await _cache.GetSchemaAsync(connection, owner, cancellationToken)).HasSchema(name))
                        return ($"(schéma) {owner}.{name}", $"Base {owner}");

                    return null;
                }

            case 2:
                {
                    var snapshot = await _cache.GetSchemaAsync(connection, scope.Database, cancellationToken);
                    if (snapshot.Find(qualifier[0], qualifier[1]) is { } table)
                        return ColumnOf(table, name);

                    if (await IsDatabaseAsync(connection, qualifier[0], cancellationToken)
                        && (await _cache.GetSchemaAsync(connection, qualifier[0], cancellationToken)).Find(qualifier[1], name) is { } remote)
                        return DescribeObject(remote, null);

                    return null;
                }

            case 3:
                {
                    var snapshot = await _cache.GetSchemaAsync(connection, qualifier[0], cancellationToken);
                    return snapshot.Find(qualifier[1], qualifier[2]) is { } table ? ColumnOf(table, name) : null;
                }

            default:
                return null;
        }
    }

    /// <summary>Objet du catalogue designe par une table du lot ; null pour une table temporaire ou inconnue.</summary>
    private async Task<DatabaseSchemaObject?> FindAsync(DatabaseConnectionInfo connection, SqlContext scope, SqlTableReference table, CancellationToken cancellationToken)
    {
        if (table.Name.StartsWith('#'))
            return null;

        var snapshot = await _cache.GetSchemaAsync(connection, table.Database ?? scope.Database, cancellationToken);
        return snapshot.Find(table.Schema, table.Name);
    }

    private async Task<bool> IsDatabaseAsync(DatabaseConnectionInfo connection, string name, CancellationToken cancellationToken)
        => (await _cache.GetDatabasesAsync(connection, cancellationToken))
            .Any(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));

    private static DatabaseSchemaColumn? FindColumn(DatabaseSchemaObject table, string name)
        => table.Columns.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    private static (string, string?)? ColumnOf(DatabaseSchemaObject table, string name)
        => FindColumn(table, name) is { } column ? DescribeColumn(table, column, []) : null;

    // ---------- Textes ----------

    /// <summary>Ex. "(colonne) Name nvarchar(50) NOT NULL", puis la table (et les autres tables du lot qui l'ont aussi).</summary>
    private static (string, string?) DescribeColumn(DatabaseSchemaObject table, DatabaseSchemaColumn column, IReadOnlyList<string> alsoIn)
    {
        var description = new StringBuilder(QualifiedName(table));
        if (column.IsPrimaryKey)
            description.Append(" · clé primaire");
        if (alsoIn.Count > 0)
            description.Append("\nAussi dans : ").AppendJoin(", ", alsoIn);

        return ($"(colonne) {column.Name} {column.DataType} {(column.IsNullable ? "NULL" : "NOT NULL")}", description.ToString());
    }

    /// <summary>Ex. "(table) dbo.Users", puis ses premieres colonnes avec leur type.</summary>
    private static (string, string?) DescribeObject(DatabaseSchemaObject source, string? alias)
    {
        var kind = source.Kind switch
        {
            DatabaseObjectKind.View => "vue",
            DatabaseObjectKind.StoredProcedure => "procédure",
            _ => "table",
        };

        var signature = alias is null || string.Equals(alias, source.Name, StringComparison.OrdinalIgnoreCase)
            ? $"({kind}) {QualifiedName(source)}"
            : $"({kind}) {QualifiedName(source)} AS {alias}";

        if (source.Columns.Count == 0)
            return (signature, null);

        var columns = source.Columns
            .Take(MaxListedColumns)
            .Select(c => $"{c.Name} {c.DataType}{(c.IsPrimaryKey ? " · clé" : string.Empty)}");

        var description = string.Join('\n', columns);
        if (source.Columns.Count > MaxListedColumns)
            description += $"\n… et {source.Columns.Count - MaxListedColumns} autre(s)";

        return (signature, description);
    }

    private static string QualifiedName(DatabaseSchemaObject source) => $"{source.Schema}.{source.Name}";

    // ---------- Variables ----------

    private static HoverInfo? DescribeVariable(string text, SqlToken[] tokens, SqlToken token, IReadOnlyList<QueryParameter> parameters)
    {
        var name = token.GetText(text);

        var parameter = parameters.FirstOrDefault(p => string.Equals(p.Name.TrimStart('@'), name.TrimStart('@'), StringComparison.OrdinalIgnoreCase));
        if (parameter is not null)
            return new HoverInfo(token.Start, token.Length, $"(paramètre) {name} {SqlTypeName(parameter.Type)}", "Paramètre du dashboard");

        return FindDeclaredType(text, tokens, name) is { } type
            ? new HoverInfo(token.Start, token.Length, $"(variable) {name} {type}", null)
            : null;
    }

    /// <summary>Type donne par un DECLARE @a int, @b nvarchar(50) = 'x' du texte ; null si la variable n'y est pas declaree.</summary>
    private static string? FindDeclaredType(string text, SqlToken[] tokens, string name)
    {
        for (var i = 0; i < tokens.Length; i++)
        {
            if (!tokens[i].IsWord(text, "DECLARE"))
                continue;

            var j = i + 1;
            while (j < tokens.Length && tokens[j].Kind == SqlTokenKind.Variable)
            {
                var variable = tokens[j].GetText(text);
                j++;

                if (j < tokens.Length && tokens[j].IsWord(text, "AS"))
                    j++;

                if (j >= tokens.Length)
                    break;

                var typeStart = tokens[j].Start;
                var isTable = tokens[j].IsWord(text, "TABLE");
                var typeEnd = tokens[j].End;
                j++;

                if (j < tokens.Length && tokens[j].Kind == SqlTokenKind.OpenParen)
                {
                    var close = SkipParens(tokens, j);
                    typeEnd = tokens[close - 1].End;
                    j = close;
                }

                if (string.Equals(variable, name, StringComparison.OrdinalIgnoreCase))
                    return isTable ? "TABLE" : string.Join(' ', text[typeStart..typeEnd].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

                // Valeur initiale eventuelle, jusqu'a la virgule qui annonce la variable suivante.
                j = SkipToNextVariable(text, tokens, j);
            }
        }

        return null;
    }

    /// <summary>Index de la variable suivante du meme DECLARE, ou au-dela de la fin si l'instruction s'arrete.</summary>
    private static int SkipToNextVariable(string text, SqlToken[] tokens, int index)
    {
        var depth = 0;
        for (var i = index; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (token.Kind == SqlTokenKind.OpenParen)
                depth++;
            else if (token.Kind == SqlTokenKind.CloseParen)
                depth--;
            else if (depth == 0 && token.Kind == SqlTokenKind.Comma && i + 1 < tokens.Length && tokens[i + 1].Kind == SqlTokenKind.Variable)
                return i + 1;
            else if (depth == 0 && (token.Kind == SqlTokenKind.Semicolon || (token.Kind == SqlTokenKind.Word && StatementWords.Contains(token.GetText(text)))))
                return tokens.Length;
        }

        return tokens.Length;
    }

    private static int SkipParens(SqlToken[] tokens, int openIndex)
    {
        var depth = 0;
        for (var i = openIndex; i < tokens.Length; i++)
        {
            if (tokens[i].Kind == SqlTokenKind.OpenParen)
                depth++;
            else if (tokens[i].Kind == SqlTokenKind.CloseParen && --depth == 0)
                return i + 1;
        }

        return tokens.Length;
    }

    /// <summary>Type SQL sous lequel le parametre est envoye.</summary>
    private static string SqlTypeName(QueryParameterType type) => type switch
    {
        QueryParameterType.Integer => "int",
        QueryParameterType.Decimal => "decimal",
        QueryParameterType.Date => "date",
        QueryParameterType.DateTime => "datetime2",
        QueryParameterType.Boolean => "bit",
        QueryParameterType.Guid => "uniqueidentifier",
        _ => "nvarchar",
    };
}
