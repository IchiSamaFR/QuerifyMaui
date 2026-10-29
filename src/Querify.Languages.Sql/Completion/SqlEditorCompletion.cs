using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;
using Quill.Code;
using Quill.Code.Completion;
using Quill.Code.Sql.Completion;
using Quill.Code.Sql.Lexing;

namespace Querify.Languages.Sql.Completion;

/// <summary>
/// Autocompletion T-SQL d'un editeur : mots-cles et objets du schema selon ce qu'attend la grammaire au curseur
/// (<see cref="SqlContextAnalyzer"/>), mots deja ecrits dans le document et parametres du dashboard.
/// </summary>
internal sealed class SqlEditorCompletion : ICodeCompletion
{
    /// <summary>Seuls mots attendus apres un nom de table : la liste doit gener le moins possible la frappe d'un alias.</summary>
    private static readonly HashSet<string> FollowTableName = new(StringComparer.OrdinalIgnoreCase)
    {
        "AS", "WITH", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "CROSS", "APPLY", "ON",
        "GROUP", "ORDER", "HAVING", "UNION", "EXCEPT", "INTERSECT", "SET", "VALUES", "SELECT", "OUTPUT", "OFFSET"
    };

    private readonly SqlKeywordCatalog _keywords;
    private readonly SqlSchemaCompletion _schema;
    private readonly DatabaseConnectionInfo? _connection;
    private readonly string? _database;
    private readonly IReadOnlyList<QueryParameter> _parameters;

    public SqlEditorCompletion(SqlKeywordCatalog keywords, SqlSchemaCompletion schema, DatabaseConnectionInfo? connection, string? database, IReadOnlyList<QueryParameter> parameters)
    {
        _keywords = keywords;
        _schema = schema;
        _connection = connection;
        _database = database;
        _parameters = parameters;
    }

    public async Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        position = Math.Clamp(position, 0, code.Length);
        var wordStart = SqlContextAnalyzer.FindWordStart(code, position);
        var context = SqlContextAnalyzer.Analyze(code, position, _database);
        if (context.Kind == SqlContextKind.None)
            return CompletionList.Empty;

        var batches = await Task.WhenAll(
            GetKeywordItemsAsync(context, cancellationToken),
            GetSchemaItemsAsync(context, cancellationToken),
            GetDocumentWordsAsync(code, wordStart, context, cancellationToken));

        return new CompletionList(Merge([.. batches, GetParameterItems(context)]), wordStart, position - wordStart);
    }

    /// <summary>Le contrat ne donne pas le curseur : le mot remplace est celui qui termine <paramref name="code"/>.</summary>
    public Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(item);

        var start = SqlContextAnalyzer.FindWordStart(code, code.Length);
        return Task.FromResult(new CompletionChange(start, code.Length - start, item.TextToInsert));
    }

    /// <summary>Table, base, membre ou procedure : place reservee aux objets du schema.</summary>
    private async Task<IReadOnlyList<CompletionItem>> GetKeywordItemsAsync(SqlContext context, CancellationToken cancellationToken)
    {
        Func<CompletionItem, bool>? accepts = context.Kind switch
        {
            SqlContextKind.General => static _ => true,
            SqlContextKind.Column => static item => item.Kind != CompletionItemKind.DataType,
            SqlContextKind.DataType => static item => item.Kind == CompletionItemKind.DataType,
            SqlContextKind.AfterTableName => static item => FollowTableName.Contains(item.Label),
            _ => null
        };

        return accepts is null ? [] : (await _keywords.GetItemsAsync(cancellationToken)).Where(accepts).ToArray();
    }

    /// <summary>Sans connexion ouverte, pas d'objet du schema.</summary>
    private Task<IReadOnlyList<CompletionItem>> GetSchemaItemsAsync(SqlContext context, CancellationToken cancellationToken)
        => _connection is { IsConnected: true } connection
            ? _schema.GetItemsAsync(connection, context, cancellationToken)
            : Task.FromResult<IReadOnlyList<CompletionItem>>([]);

    /// <summary>
    /// Identifiants deja ecrits (tables, colonnes, alias), comme les suggestions par mot de VS Code : utile hors
    /// connexion. Le mot en cours de frappe est exclu : il se proposerait lui-meme.
    /// </summary>
    private async Task<IReadOnlyList<CompletionItem>> GetDocumentWordsAsync(string code, int wordStart, SqlContext context, CancellationToken cancellationToken)
    {
        if (context.Kind is SqlContextKind.DataType or SqlContextKind.AfterTableName)
            return [];

        var keywords = await _keywords.GetWordsAsync(cancellationToken);
        return SqlLexer.Tokenize(code)
                       .Where(token => token.Kind is SqlTokenKind.Word or SqlTokenKind.QuotedIdentifier && token.Start != wordStart)
                       .Select(token => token.GetText(code))
                       .Where(word => word.Length > 1 && !keywords.Contains(word))
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .Select(word => new CompletionItem { Label = word, Kind = CompletionItemKind.Text })
                       .ToArray();
    }

    /// <summary>Parametres du dashboard, en @Nom sans DECLARE : une valeur s'ecrit dans une expression, pas a la place d'une table.</summary>
    private IReadOnlyList<CompletionItem> GetParameterItems(SqlContext context)
    {
        if (_parameters.Count == 0 || context.Kind is not (SqlContextKind.General or SqlContextKind.Column))
            return [];

        return _parameters
            .Select(p => new CompletionItem
            {
                Label = "@" + p.Name,
                Kind = CompletionItemKind.Variable,
                Detail = p.Type.ToString(),
            })
            .ToArray();
    }

    private static CompletionItem[] Merge(IReadOnlyList<CompletionItem>[] batches)
    {
        // MAX est a la fois type et fonction, Users peut exister dans deux schemas : seuls les vrais doublons partent.
        var seen = new HashSet<(string, CompletionItemKind, string?)>();
        var items = new List<CompletionItem>(batches.Sum(batch => batch.Count));
        foreach (var item in batches.SelectMany(batch => batch))
        {
            if (seen.Add((item.Label.ToUpperInvariant(), item.Kind, item.Detail)))
                items.Add(item);
        }

        // Un mot du document deja connu du schema ou des mots-cles ferait doublon, sans type ni icone.
        var known = items.Where(item => item.Kind != CompletionItemKind.Text)
                         .Select(item => item.Label)
                         .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return items.Where(item => item.Kind != CompletionItemKind.Text || !known.Contains(item.Label.Trim('[', ']')))
                    .ToArray();
    }
}
