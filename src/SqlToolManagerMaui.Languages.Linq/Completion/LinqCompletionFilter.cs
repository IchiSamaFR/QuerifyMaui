using Microsoft.CodeAnalysis.Tags;
using SqlToolManagerMaui.Languages.Linq.Modeling;

namespace SqlToolManagerMaui.Languages.Linq.Completion;

/// <summary>
/// Reduit les suggestions Roslyn a ce qui sert dans une requete LINQ / EF : variables, mots-cles
/// de requete, quelques types utiles et entites generees en debut d'expression ; apres un point,
/// les membres sans ceux d'object ni les variantes Async, et seulement les DbSet sur le contexte.
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

    private static readonly HashSet<string> VariableKinds = new(StringComparer.Ordinal) { WellKnownTags.Local, WellKnownTags.Parameter, WellKnownTags.RangeVariable };

    private static readonly HashSet<string> TypeKinds = new(StringComparer.Ordinal) { WellKnownTags.Class, WellKnownTags.Structure, WellKnownTags.Enum, WellKnownTags.Interface, WellKnownTags.Delegate };

    private readonly bool _isMemberAccess;
    private readonly IReadOnlySet<string>? _contextMembers;
    private readonly HashSet<string> _entities;

    /// <param name="isMemberAccess">Mot complete apres un point.</param>
    /// <param name="contextMembers">DbSet du contexte si l'expression avant le point en est un, null sinon.</param>
    public LinqCompletionFilter(EfGeneratedModel? model, bool isMemberAccess, IReadOnlySet<string>? contextMembers)
    {
        _isMemberAccess = isMemberAccess;
        _contextMembers = contextMembers;
        _entities = new HashSet<string>(model?.EntityClassNames ?? [], StringComparer.Ordinal);
    }

    /// <param name="name">Texte insere (DisplayText Roslyn, sans suffixe "&lt;&gt;").</param>
    /// <param name="kind">Nature retenue par <see cref="LinqCompletionEngine"/>.</param>
    public bool Keeps(string name, string kind)
        => _isMemberAccess ? KeepsMember(name, kind) : KeepsInExpression(name, kind);

    private bool KeepsInExpression(string name, string kind)
    {
        // Parametres internes du source genere (__context, __dump).
        if (VariableKinds.Contains(kind))
            return !name.StartsWith("__", StringComparison.Ordinal);

        if (kind == WellKnownTags.Keyword)
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
        if (kind is WellKnownTags.Method or WellKnownTags.ExtensionMethod && name.EndsWith("Async", StringComparison.Ordinal))
            return false;

        return _contextMembers is null || _contextMembers.Contains(name);
    }
}
