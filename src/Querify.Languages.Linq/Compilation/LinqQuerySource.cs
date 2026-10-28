using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;
using Quill.Code.CSharp.Workspaces;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Querify.Languages.Linq.Compilation;

/// <summary>
/// Source C# d'une requete LINQ (sans etat) : methode generee autour de la requete, usings, references et options
/// de compilation. Partage par l'editeur et l'execution.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "App non publiee en trimmed : compilation assumee.")]
internal static class LinqQuerySource
{
    internal const string QueryNamespace = "Querify.UserQueries";
    internal const string QueryClass = "UserQuery";
    internal const string QueryMethod = "Run";

    /// <summary>Nom de fichier virtuel des diagnostics : les positions sont celles du texte de l'onglet.</summary>
    private const string QueryFileName = "requete";

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

    /// <inheritdoc cref="ParseOptions"/>
    internal static CSharpCompilationOptions CompilationOptions { get; } = new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable);

    /// <summary>Usings de la requete, document fixe du projet : la requete ne peut pas en declarer.</summary>
    internal static string BuildGlobalUsings(EfGeneratedModel? model)
    {
        var code = new StringBuilder();
        code.AppendLine("global using System;");
        code.AppendLine("global using System.Collections.Generic;");
        code.AppendLine("global using System.Linq;");
        code.AppendLine("global using Microsoft.EntityFrameworkCore;");
        if (model is not null)
            code.AppendLine($"global using {model.Namespace};");

        return code.ToString();
    }

    /// <summary>
    /// La requete devient le corps d'une methode ou une variable porte le nom de la base :
    /// une expression seule est retournee telle quelle (le ";" final est tolere), des
    /// instructions avec "return" forment le corps lui-meme (voir <see cref="IsStatementBlock"/>).
    /// #line recale les diagnostics sur le texte de l'onglet.
    /// Sans modele (pas de connexion), le source reste valide pour l'autocompletion C#.
    /// Usings : document fixe du projet (<see cref="BuildGlobalUsings"/>) ; Dump et le contexte viennent du modele, Log de Quill.
    /// </summary>
    /// <returns>Le source et la position du texte de la requete dans ce source.</returns>
    internal static (string Source, int QueryStart) Build(EfGeneratedModel? model, string query, IReadOnlyList<LinqParameter>? parameters = null)
    {
        var code = new StringBuilder();
        code.AppendLine($"namespace {QueryNamespace};");
        code.AppendLine();
        code.AppendLine($"public static class {QueryClass}");
        code.AppendLine("{");
        code.AppendLine($"    public static object {QueryMethod}(object[] __args)");
        code.AppendLine("    {");
        if (model is not null)
            code.AppendLine($"        var {model.ContextVariableName} = global::{model.Namespace}.{model.ContextClassName}.{EfModelGenerator.ContextPropertyName};");

        // Prefixe @ : un nom de parametre peut etre un mot cle C#, et la requete peut ecrire @Nom comme en SQL.
        for (var i = 0; i < (parameters?.Count ?? 0); i++)
        {
            var parameter = parameters![i];
            if (!SyntaxFacts.IsValidIdentifier(parameter.Name))
                throw new ArgumentException($"« {parameter.Name} » n'est pas un nom de variable C# valide.", nameof(parameters));

            code.AppendLine($"        var @{parameter.Name} = ({TypeName(parameter.Type)})__args[{i}];");
        }

        var isStatementBlock = IsStatementBlock(query);
        if (!isStatementBlock)
            code.AppendLine("        return");

        code.AppendLine($"#line 1 \"{QueryFileName}\"");

        var queryStart = code.Length;
        code.AppendLine(query);
        code.AppendLine("#line default");

        // Instructions sans return sur tous les chemins : rien a afficher plutot qu'une erreur CS0161.
        code.AppendLine(isStatementBlock ? "        return null;" : "        ;");
        code.AppendLine("    }");
        code.AppendLine("}");
        return (code.ToString(), queryStart);
    }

    /// <summary>
    /// Instructions plutot qu'expression : un "return", une declaration en tete (var ids = ...)
    /// ou un ";" suivi d'autre chose. Une expression terminee par ";" reste une expression.
    /// </summary>
    internal static bool IsStatementBlock(string query)
    {
        var tokens = SyntaxFactory.ParseTokens(query)
            .Where(t => !t.IsKind(SyntaxKind.EndOfFileToken))
            .ToArray();

        if (tokens.Length == 0)
            return false;

        if ((tokens[0].IsKind(SyntaxKind.IdentifierToken) && tokens[0].ValueText == "var")
            || tokens.Any(t => t.IsKind(SyntaxKind.ReturnKeyword)))
            return true;

        return tokens.SkipLast(1).Any(t => t.IsKind(SyntaxKind.SemicolonToken));
    }

    /// <summary>Une expression rend toujours une valeur ; des instructions, seulement si elles contiennent un return.</summary>
    internal static bool ReturnsValue(string query)
        => !IsStatementBlock(query) || SyntaxFactory.ParseTokens(query).Any(t => t.IsKind(SyntaxKind.ReturnKeyword));

    /// <summary>Nom C# compilable d'un type de parametre ; Roslyn l'affiche de toute facon en mot cle (int?, string).</summary>
    private static string TypeName(Type type)
        => Nullable.GetUnderlyingType(type) is { } underlying
            ? TypeName(underlying) + "?"
            : "global::" + (type.FullName ?? type.Name).Replace('+', '.');

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

