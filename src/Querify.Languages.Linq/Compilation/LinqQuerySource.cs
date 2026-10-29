using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;
using Quill.Code.CSharp;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Querify.Languages.Linq.Compilation;

/// <summary>
/// Cadre C# des requetes LINQ (sans etat) : usings et alias du contexte, globals (contexte et parametres), references
/// et options de compilation. Partage par l'editeur et l'execution.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "App non publiee en trimmed : compilation assumee.")]
internal static class LinqQuerySource
{
    /// <summary>Alias du DbContext genere : une requete reste valable d'une base a l'autre.</summary>
    internal const string ContextAlias = "QueryDbContext";

    /// <summary>Classe statique generee dans chaque requete : le contexte et les parametres du dashboard, utilisables sans les declarer.</summary>
    internal const string GlobalsClass = "QueryGlobals";

    /// <summary>Nom, dans <see cref="GlobalsClass"/>, du contexte de la requete.</summary>
    internal const string ContextGlobal = "Db";

    /// <summary>
    /// Nom du document fixe qui porte <see cref="GlobalsClass"/> et ses usings implicites (dont l'alias du contexte).
    /// </summary>
    internal const string GlobalUsings = "GlobalUsings.cs";

    /// <summary>Nom du document fixe qui porte <see cref="GlobalsClass"/>.</summary>
    internal const string GlobalDocuments = "GlobalDocuments.cs";

    private static readonly Dictionary<Type, string> TypeKeywords = new()
    {
        [typeof(int)] = "int",
        [typeof(long)] = "long",
        [typeof(short)] = "short",
        [typeof(byte)] = "byte",
        [typeof(bool)] = "bool",
        [typeof(decimal)] = "decimal",
        [typeof(double)] = "double",
        [typeof(float)] = "float",
        [typeof(string)] = "string",
        [typeof(object)] = "object",
    };

    /// <summary>
    /// Assemblies de la plateforme et d'EF contre lesquelles on compile, prises dans le runtime
    /// charge (comme LINQPad) : pas de packs de reference a embarquer. Liste couteuse : calculee une fois.
    /// </summary>
    private static readonly Lazy<IReadOnlyList<MetadataReference>> PlatformReferencesCache = new(LoadPlatformReferences);

    /// <summary>References communes a la compilation et a l'autocompletion.</summary>
    internal static IReadOnlyList<MetadataReference> PlatformReferences => PlatformReferencesCache.Value;

    private static readonly string[] ReferencedAssemblyPrefixes =
    [
        "System.", "mscorlib", "netstandard", "Microsoft.CSharp", "Microsoft.EntityFrameworkCore", "Microsoft.Extensions.", "Microsoft.Data.SqlClient"
    ];

    /// <summary>Options communes au modele, aux requetes executees et au workspace de l'editeur.</summary>
    internal static CSharpParseOptions ParseOptions { get; } = new(LanguageVersion.Latest);

    /// <summary>
    /// Une requete est un programme C# natif (instructions de haut niveau, comme Program.cs) : executable, donc un point
    /// d'entree genere par Roslyn. Le texte est compile tel quel, sans classe ni methode ajoutee.
    /// </summary>
    internal static CSharpCompilationOptions CompilationOptions { get; } = new(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Disable);

    /// <summary>Le modele genere n'a pas de point d'entree : une bibliotheque.</summary>
    internal static CSharpCompilationOptions ModelCompilationOptions { get; } = new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable);

    /// <summary>Erreur Roslyn d'un programme sans instruction ni Main : pas une faute dans l'editeur, ou un texte vide la signalerait.</summary>
    internal const string NoEntryPointCode = "CS5001";

    /// <summary>Document fixe du projet : <see cref="GlobalsClass"/> avec le contexte et un membre par parametre, imports en tete (using static global).</summary>
    internal static string BuildGlobals(IReadOnlyList<LinqParameter> parameters)
    {
        var code = new StringBuilder();
        code.AppendLine($"global using static {GlobalsClass};");
        code.AppendLine($"public static class {GlobalsClass}");
        code.AppendLine("{");
        code.AppendLine($"    public static {ContextAlias} {ContextGlobal} {{ get; internal set; }}");

        // La valeur est donnee a la creation de la classe : rien a lier ensuite.
        foreach (var (name, parameter) in GetGlobalParameters(parameters))
            code.AppendLine($"    public static {ToCSharpName(parameter.Type)} {name} {{ get; }}{ToInitializer(name, parameter)}");

        code.AppendLine("}");
        return code.ToString();
    }

    /// <summary>
    /// Parametres exposes par <see cref="GlobalsClass"/>, sous leur nom sans @. Un nom qui n'est pas un identifiant C#,
    /// un mot reserve ou un doublon n'est pas expose : le script ne peut pas le nommer.
    /// </summary>
    internal static IReadOnlyList<(string Name, LinqParameter Parameter)> GetGlobalParameters(IReadOnlyList<LinqParameter> parameters)
    {
        var used = new HashSet<string>(StringComparer.Ordinal) { ContextGlobal, GlobalsClass };
        List<(string, LinqParameter)> globals = [];

        foreach (var parameter in parameters)
        {
            var name = parameter.Name.TrimStart('@');
            if (SyntaxFacts.IsValidIdentifier(name) && SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None && used.Add(name))
                globals.Add((name, parameter));
        }

        return globals;
    }

    /// <summary>Initialiseur de la propriete (" = 16;"), vide sans valeur. Une valeur qui ne convient pas au type leve <see cref="ArgumentException"/>.</summary>
    private static string ToInitializer(string name, LinqParameter parameter)
    {
        if (parameter.Value is null)
            return string.Empty;

        var type = Nullable.GetUnderlyingType(parameter.Type) ?? parameter.Type;

        try
        {
            object value = parameter.Value switch
            {
                _ when type.IsInstanceOfType(parameter.Value) => parameter.Value,
                string text when type == typeof(Guid) => Guid.Parse(text),
                DateTime date when type == typeof(DateOnly) => DateOnly.FromDateTime(date),
                _ when type.IsEnum => Enum.ToObject(type, parameter.Value),
                _ => Convert.ChangeType(parameter.Value, type, CultureInfo.InvariantCulture),
            };

            var literal = value switch
            {
                DateTime date => $"new global::System.DateTime({date.Ticks}L, global::System.DateTimeKind.{date.Kind})",
                DateOnly date => $"global::System.DateOnly.FromDayNumber({date.DayNumber})",
                Guid guid => $"global::System.Guid.Parse(\"{guid}\")",
                _ when type.IsEnum => $"({ToCSharpName(type)}){Convert.ToInt64(value, CultureInfo.InvariantCulture)}",
                _ => SymbolDisplay.FormatPrimitive(value, quoteStrings: true, useHexadecimalNumbers: false),
            };

            return literal is null ? throw new InvalidCastException() : $" = {literal};";
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
        {
            throw new ArgumentException($"Parametre « {name} » : la valeur « {parameter.Value} » ne convient pas au type {type.Name}.", nameof(parameter), ex);
        }
    }

    private static string ToCSharpName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return ToCSharpName(underlying) + "?";

        return TypeKeywords.TryGetValue(type, out var keyword) ? keyword : $"global::{type.FullName}";
    }

    /// <summary>Usings implicites et alias du contexte, document fixe du projet ; la requete peut declarer les siens.</summary>
    internal static string BuildGlobalUsings(EfGeneratedModel? model)
    {
        var code = new StringBuilder();
        code.AppendLine("global using System;");
        code.AppendLine("global using System.Collections.Generic;");
        code.AppendLine("global using System.Linq;");
        code.AppendLine("global using System.Threading.Tasks;");
        code.AppendLine("global using Microsoft.EntityFrameworkCore;");

        // Sans connexion, l'alias designe DbContext : la requete reste analysable.
        if (model is null)
        {
            code.AppendLine($"global using {ContextAlias} = global::Microsoft.EntityFrameworkCore.DbContext;");
        }
        else
        {
            code.AppendLine($"global using {model.Namespace};");
            code.AppendLine($"global using {ContextAlias} = global::{model.Namespace}.{model.ContextClassName};");
        }

        return code.ToString();
    }

    private static IReadOnlyList<MetadataReference> LoadPlatformReferences()
    {
        // Assemblies du runtime (a defaut, celles deja chargees).
        var paths = CSharpReferences.GetTrustedPlatformAssemblyPaths();

        // EF n'est peut-etre pas encore charge : on force ses assemblies dans la liste.
        string[] required = [typeof(DbContext).Assembly.Location, typeof(SqlServerDbContextOptionsExtensions).Assembly.Location, typeof(RelationalQueryableExtensions).Assembly.Location];

        return paths.Concat(required)
                    .Where(path => !string.IsNullOrEmpty(path) && ReferencedAssemblyPrefixes.Any(p => Path.GetFileName(path).StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                    .ToArray();
    }
}

