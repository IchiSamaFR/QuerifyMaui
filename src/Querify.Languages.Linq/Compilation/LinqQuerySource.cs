using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Querify.Languages.Linq.Modeling;
using Quill.Code.CSharp.Workspaces;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Querify.Languages.Linq.Compilation;

/// <summary>
/// Cadre C# des requetes LINQ (sans etat) : point d'entree attendu, usings et alias du contexte, references et
/// options de compilation. Partage par l'editeur et l'execution ; la requete est un fichier C# complet.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "App non publiee en trimmed : compilation assumee.")]
internal static class LinqQuerySource
{
    /// <summary>Classe que declare la requete ; sa methode <see cref="EntryMethod"/> est appelee.</summary>
    internal const string EntryClass = "UserQuery";

    /// <inheritdoc cref="EntryClass"/>
    internal const string EntryMethod = "Run";

    /// <summary>Alias du DbContext genere : une requete reste valable d'une base a l'autre.</summary>
    internal const string ContextAlias = "QueryDbContext";

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

