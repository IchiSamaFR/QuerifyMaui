using SqlToolManagerMaui.Core.Enums.Databases.Connections;
using SqlToolManagerMaui.Core.Enums.Editors.Completion;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Databases.Schemas;
using SqlToolManagerMaui.Core.Models.Editors.Completion;
using SqlToolManagerMaui.Databases.Schemas;
using SqlToolManagerMaui.Languages.Commons.Editors.Completion;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace SqlToolManagerMaui.Services.Editors.Completion;

/// <summary>
/// Bases, schemas, tables, vues, procedures et colonnes de la connexion de l'onglet,
/// selon le contexte : FROM -> tables, alias. -> colonnes, USE -> bases, EXEC -> procedures.
/// </summary>
[AutoRegister(As = typeof(ICompletionProvider))]
public sealed partial class SchemaCompletionProvider : ICompletionProvider
{
    private const string DefaultSchema = "dbo";

    private readonly IDatabaseSchemaCache _cache;
    private readonly ISyntaxConfigurationStore _configurations;
    private HashSet<string>? _reservedWords;

    public SchemaCompletionProvider(IDatabaseSchemaCache cache, ISyntaxConfigurationStore configurations)
    {
        _cache = cache;
        _configurations = configurations;
    }

    [GeneratedRegex(@"^[\p{L}_][\p{L}\p{Nd}_@#$]*$", RegexOptions.CultureInvariant)]
    private static partial Regex RegularIdentifierRegex();

    public async Task<IReadOnlyList<CompletionItem>> GetItemsAsync(CompletionRequest request, CompletionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        // Le schema SQL n'a pas de sens en LINQ : c'est le DbContext genere qui y repondra.
        if (request.Language != SyntaxLanguage.Sql || request.Connection is not { IsConnected: true } connection)
            return [];

        _reservedWords ??= await LoadReservedWordsAsync(request.Language, cancellationToken);

        try
        {
            return context.Kind switch
            {
                CompletionContextKind.Database => await GetDatabaseItemsAsync(connection, cancellationToken),
                CompletionContextKind.TableSource => await GetSourceItemsAsync(connection, context.Database, DataSourceKinds, cancellationToken),
                CompletionContextKind.Procedure => await GetSourceItemsAsync(connection, context.Database, ProcedureKinds, cancellationToken),
                CompletionContextKind.Column => await GetQueryColumnItemsAsync(connection, context, cancellationToken),
                CompletionContextKind.MemberAccess => await GetMemberItemsAsync(connection, context, cancellationToken),
                _ => []
            };
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or TimeoutException)
        {
            // Serveur injoignable ou droits insuffisants : la completion se rabat sur les mots-cles.
            return [];
        }
    }

    /// <summary>Catalogue de la base par defaut et liste des bases charges des la connexion, pas a la premiere frappe.</summary>
    public async Task WarmUpAsync(SyntaxLanguage language, DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
    {
        if (language != SyntaxLanguage.Sql || connection is not { IsConnected: true })
            return;

        _reservedWords ??= await LoadReservedWordsAsync(language, cancellationToken);
        await Task.WhenAll(
            _cache.GetSchemaAsync(connection, null, cancellationToken),
            _cache.GetDatabasesAsync(connection, cancellationToken));
    }

    private static readonly DatabaseObjectKind[] DataSourceKinds = [DatabaseObjectKind.Table, DatabaseObjectKind.View];

    private static readonly DatabaseObjectKind[] ProcedureKinds = [DatabaseObjectKind.StoredProcedure];

    // ---------- Par contexte ----------

    private async Task<IReadOnlyList<CompletionItem>> GetDatabaseItemsAsync(DatabaseConnectionInfo connection, CancellationToken cancellationToken)
    {
        var databases = await _cache.GetDatabasesAsync(connection, cancellationToken);

        return databases.Select(d => new CompletionItem
        {
            Label = d.Name,
            Kind = CompletionItemKind.Database,
            InsertText = Quote(d.Name),
            Detail = d.IsSystem ? "systeme" : null
        }).ToArray();
    }

    /// <summary>Objets et schemas de la base courante, plus les autres bases pour ecrire Base.schema.objet.</summary>
    private async Task<IReadOnlyList<CompletionItem>> GetSourceItemsAsync(DatabaseConnectionInfo connection, string? database,
        DatabaseObjectKind[] kinds, CancellationToken cancellationToken)
    {
        var objects = await GetObjectItemsAsync(connection, database, null, kinds, withSchemas: true, cancellationToken);
        var databases = await GetDatabaseItemsAsync(connection, cancellationToken);

        return objects.Concat(databases).ToArray();
    }

    /// <summary>Objets d'une base, d'un schema si precise ; les schemas eux-memes pour enchainer sur "schema.".</summary>
    private async Task<IReadOnlyList<CompletionItem>> GetObjectItemsAsync(DatabaseConnectionInfo connection, string? database, string? schema,
        DatabaseObjectKind[] kinds, bool withSchemas, CancellationToken cancellationToken)
    {
        var snapshot = await _cache.GetSchemaAsync(connection, database, cancellationToken);

        var objects = snapshot.Objects
            .Where(o => kinds.Contains(o.Kind))
            .Where(o => schema is null || string.Equals(o.Schema, schema, StringComparison.OrdinalIgnoreCase))
            .Select(o => new CompletionItem
            {
                Label = o.Name,
                Kind = ToItemKind(o.Kind),
                // Hors dbo, le schema est insere : "Users" seul viserait dbo.Users.
                InsertText = schema is not null || IsDefaultSchema(o.Schema) ? Quote(o.Name) : $"{Quote(o.Schema)}.{Quote(o.Name)}",
                Detail = o.Schema
            });

        if (!withSchemas)
            return objects.ToArray();

        var schemas = snapshot.Schemas.Select(s => new CompletionItem
        {
            Label = s,
            Kind = CompletionItemKind.Schema,
            InsertText = Quote(s),
            Detail = snapshot.Database
        });

        return objects.Concat(schemas).ToArray();
    }

    /// <summary>Colonnes de toutes les tables citees dans la requete (SELECT, WHERE, ON...).</summary>
    private async Task<IReadOnlyList<CompletionItem>> GetQueryColumnItemsAsync(DatabaseConnectionInfo connection, CompletionContext context, CancellationToken cancellationToken)
    {
        List<CompletionItem> items = [];

        foreach (var table in context.Tables)
        {
            if (await ResolveAsync(connection, context, table, cancellationToken) is not { } resolved)
                continue;

            items.AddRange(ColumnItems(resolved, table.ExposedName));
        }

        return items;
    }

    /// <summary>
    /// Apres un point : alias ou table -> colonnes, schema -> objets, base -> schemas,
    /// base.schema -> objets, [base.]schema.table -> colonnes.
    /// </summary>
    private async Task<IReadOnlyList<CompletionItem>> GetMemberItemsAsync(DatabaseConnectionInfo connection, CompletionContext context, CancellationToken cancellationToken)
    {
        var qualifier = context.Qualifier;
        var kinds = context.MemberParentKind == CompletionContextKind.Procedure ? ProcedureKinds : DataSourceKinds;

        switch (qualifier.Count)
        {
            case 1:
                {
                    var name = qualifier[0];

                    if (context.FindTable(name) is { } table && await ResolveAsync(connection, context, table, cancellationToken) is { } aliased)
                        return ColumnItems(aliased, null);

                    var snapshot = await _cache.GetSchemaAsync(connection, context.Database, cancellationToken);
                    if (snapshot.HasSchema(name))
                        return await GetObjectItemsAsync(connection, context.Database, name, kinds, withSchemas: false, cancellationToken);

                    // Table non citee dans la requete : SELECT Users.| est valide si Users est dans le FROM d'une autre instruction.
                    if (snapshot.Find(null, name) is { } direct && direct.Columns.Count > 0)
                        return ColumnItems(direct, null);

                    if (await IsDatabaseAsync(connection, name, cancellationToken))
                        return SchemaItems(await _cache.GetSchemaAsync(connection, name, cancellationToken));

                    return [];
                }

            case 2:
                {
                    var snapshot = await _cache.GetSchemaAsync(connection, context.Database, cancellationToken);
                    if (snapshot.Find(qualifier[0], qualifier[1]) is { } table)
                        return ColumnItems(table, null);

                    return await IsDatabaseAsync(connection, qualifier[0], cancellationToken)
                        ? await GetObjectItemsAsync(connection, qualifier[0], qualifier[1], kinds, withSchemas: false, cancellationToken)
                        : [];
                }

            case 3:
                {
                    var snapshot = await _cache.GetSchemaAsync(connection, qualifier[0], cancellationToken);
                    return snapshot.Find(qualifier[1], qualifier[2]) is { } table ? ColumnItems(table, null) : [];
                }

            default:
                return [];
        }
    }

    // ---------- Resolution ----------

    /// <summary>Objet du catalogue designe par une reference de la requete ; null pour une table temporaire ou inconnue.</summary>
    private async Task<DatabaseSchemaObject?> ResolveAsync(DatabaseConnectionInfo connection, CompletionContext context, SqlTableReference table, CancellationToken cancellationToken)
    {
        if (table.Name.StartsWith('#'))
            return null;

        var snapshot = await _cache.GetSchemaAsync(connection, table.Database ?? context.Database, cancellationToken);
        return snapshot.Find(table.Schema, table.Name);
    }

    private async Task<bool> IsDatabaseAsync(DatabaseConnectionInfo connection, string name, CancellationToken cancellationToken)
        => (await _cache.GetDatabasesAsync(connection, cancellationToken))
            .Any(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));

    // ---------- Construction des suggestions ----------

    private IReadOnlyList<CompletionItem> ColumnItems(DatabaseSchemaObject table, string? owner)
        => table.Columns.Select(c => new CompletionItem
        {
            Label = c.Name,
            Kind = CompletionItemKind.Column,
            InsertText = Quote(c.Name),
            // Plusieurs tables dans la requete : on precise d'ou vient la colonne.
            Detail = owner is null ? c.DataType : $"{c.DataType} · {owner}"
        }).ToArray();

    private IReadOnlyList<CompletionItem> SchemaItems(DatabaseSchemaSnapshot snapshot)
        => snapshot.Schemas.Select(s => new CompletionItem
        {
            Label = s,
            Kind = CompletionItemKind.Schema,
            InsertText = Quote(s),
            Detail = snapshot.Database
        }).ToArray();

    private static CompletionItemKind ToItemKind(DatabaseObjectKind kind) => kind switch
    {
        DatabaseObjectKind.Table => CompletionItemKind.Table,
        DatabaseObjectKind.View => CompletionItemKind.View,
        _ => CompletionItemKind.Procedure,
    };

    private static bool IsDefaultSchema(string schema) => string.Equals(schema, DefaultSchema, StringComparison.OrdinalIgnoreCase);

    /// <summary>Crochets pour un nom irregulier (espace, tiret...) ou reserve (Order, User).</summary>
    private string Quote(string name)
        => RegularIdentifierRegex().IsMatch(name) && _reservedWords?.Contains(name) != true
            ? name
            : $"[{name.Replace("]", "]]", StringComparison.Ordinal)}]";

    /// <summary>Seuls les mots-cles reserves : une colonne Year ou Date n'a pas besoin de crochets.</summary>
    private async Task<HashSet<string>> LoadReservedWordsAsync(SyntaxLanguage language, CancellationToken cancellationToken)
    {
        var configuration = await _configurations.GetAsync(language, cancellationToken);
        return new HashSet<string>(configuration?.Definition.Keywords.Where(k => k.Category == "Keyword").Select(k => k.Word) ?? [], StringComparer.OrdinalIgnoreCase);
    }
}
