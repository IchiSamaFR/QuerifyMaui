namespace SqlToolManagerMaui.Languages.Linq;

/// <summary>
/// Reduit les suggestions Roslyn a ce qui sert dans une requete LINQ / EF : variables, mots-cles
/// de requete, quelques types utiles et entites generees en debut d'expression ; apres un point,
/// les membres sans la plomberie d'object, de DbContext ni les variantes Async.
/// </summary>
internal sealed class LinqCompletionFilter
{
    /// <summary>Mots-cles C# utiles a une requete (syntaxe de requete, instructions, litteraux, types simples).</summary>
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "var", "return", "new", "from", "in", "where", "select", "orderby", "ascending", "descending",
        "group", "by", "into", "join", "on", "equals", "let", "true", "false", "null", "is", "not", "and", "or",
        "as", "if", "else", "foreach", "default", "nameof",
        "string", "int", "long", "short", "byte", "decimal", "double", "float", "bool", "char", "object"
    };

    /// <summary>Types frequents dans un filtre ou une projection.</summary>
    private static readonly HashSet<string> Types = new(StringComparer.Ordinal)
    {
        "DateTime", "DateTimeOffset", "DateOnly", "TimeOnly", "TimeSpan", "Guid", "Math", "Convert", "EF", "List", "Console"
    };

    /// <summary>Membres d'object herites par tout : bruit dans une requete.</summary>
    private static readonly HashSet<string> ObjectMembers = new(StringComparer.Ordinal)
    {
        "Equals", "GetHashCode", "GetType", "ReferenceEquals", "MemberwiseClone"
    };

    /// <summary>Membres du DbContext hors DbSet : ecriture, suivi, metadonnees.</summary>
    private static readonly HashSet<string> ContextPlumbing = new(StringComparer.Ordinal)
    {
        "Add", "AddRange", "Attach", "AttachRange", "Update", "UpdateRange", "Remove", "RemoveRange",
        "Entry", "Find", "SaveChanges", "Dispose", "ChangeTracker", "Database", "Model", "ContextId",
        "SavingChanges", "SavedChanges", "SaveChangesFailed", "FromExpression"
    };

    private static readonly HashSet<string> VariableKinds = new(StringComparer.Ordinal) { "Local", "Parameter", "RangeVariable" };

    private static readonly HashSet<string> TypeKinds = new(StringComparer.Ordinal) { "Class", "Structure", "Enum", "Interface", "Delegate" };

    private readonly EfGeneratedModel? _model;
    private readonly string? _memberOf;
    private readonly HashSet<string> _entities;

    /// <param name="memberOf">Identifiant avant le point (vide si expression complexe), null hors acces membre.</param>
    public LinqCompletionFilter(EfGeneratedModel? model, string? memberOf)
    {
        _model = model;
        _memberOf = memberOf;
        _entities = new HashSet<string>(model?.EntityClassNames ?? [], StringComparer.Ordinal);
    }

    /// <param name="name">Texte insere (DisplayText Roslyn, sans suffixe "&lt;&gt;").</param>
    /// <param name="kind">Nature retenue par <see cref="LinqCompletionEngine"/>.</param>
    public bool Keeps(string name, string kind)
        => _memberOf is null ? KeepsInExpression(name, kind) : KeepsMember(name, kind);

    private bool KeepsInExpression(string name, string kind)
    {
        // Parametres internes du source genere (__context, __dump).
        if (VariableKinds.Contains(kind))
            return !name.StartsWith("__", StringComparison.Ordinal);

        if (kind == "Keyword")
            return Keywords.Contains(name);

        if (TypeKinds.Contains(kind))
            return Types.Contains(name) || _entities.Contains(name);

        // Espaces de noms, snippets, classes generees du source (UserQuery...), le reste.
        return false;
    }

    private bool KeepsMember(string name, string kind)
    {
        // Collecteurs internes du source genere (Console.__sink...).
        if (ObjectMembers.Contains(name) || name.StartsWith("__", StringComparison.Ordinal))
            return false;

        // ToListAsync, FirstOrDefaultAsync... : l'execution est synchrone, ils ne font qu'allonger la liste.
        if (kind is "Method" or "ExtensionMethod" && name.EndsWith("Async", StringComparison.Ordinal))
            return false;

        if (_model is not null && string.Equals(_memberOf, _model.ContextVariableName, StringComparison.Ordinal))
            return !ContextPlumbing.Contains(name);

        return true;
    }
}
