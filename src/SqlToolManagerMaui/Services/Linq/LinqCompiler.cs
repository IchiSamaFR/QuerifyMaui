using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;

namespace SqlToolManagerMaui.Services.Linq;

/// <inheritdoc cref="ILinqCompiler"/>
[AutoRegister(As = typeof(ILinqCompiler))]
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "App non publiee en trimmed : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "App non publiee en trimmed : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "App non publiee en trimmed : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "App non publiee en AOT : compilation et reflexion assumees.")]
public sealed class LinqCompiler : ILinqCompiler
{
    private const string QueryNamespace = "SqlToolManagerMaui.UserQueries";
    private const string QueryClass = "UserQuery";
    private const string QueryMethod = "Run";

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

    public LinqModelAssembly CompileModel(EfGeneratedModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var image = Emit($"EfModel_{Guid.NewGuid():N}", model.Source, PlatformReferences, out var errors);
        if (image is null)
            throw new LinqCompilationException($"Le modele genere pour {model.ContextVariableName} ne compile pas.", errors);

        // Contexte non collectable : le modele vit tant que l'application, et EF garde ses types en cache.
        var context = new AssemblyLoadContext($"EfModel:{model.ContextVariableName}", isCollectible: false);
        var assembly = context.LoadFromStream(new MemoryStream(image));

        return new LinqModelAssembly
        {
            Model = model,
            Assembly = assembly,
            ContextType = assembly.GetType($"{model.Namespace}.{model.ContextClassName}", throwOnError: true)!,
            Reference = MetadataReference.CreateFromImage(image)
        };
    }

    public LinqCompiledQuery CompileQuery(LinqModelAssembly model, string query)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(query);

        var (source, _) = BuildQuerySource(model.Model, query);
        var image = Emit($"LinqQuery_{Guid.NewGuid():N}", source, [.. PlatformReferences, model.Reference], out var errors);
        if (image is null)
            return new LinqCompiledQuery { Errors = errors };

        var assembly = new QueryLoadContext(model.Assembly).LoadFromStream(new MemoryStream(image));
        var method = assembly.GetType($"{QueryNamespace}.{QueryClass}", throwOnError: true)!.GetMethod(QueryMethod)!;

        return new LinqCompiledQuery { Run = method.CreateDelegate<Func<DbContext, object?>>() };
    }

    public DbContext CreateContext(LinqModelAssembly model, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(model);

        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(typeof(DbContextOptionsBuilder<>).MakeGenericType(model.ContextType))!;
        builder.UseSqlServer(connectionString)
               // Lecture seule : pas de suivi des entites, inutile pour afficher des resultats.
               .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        return (DbContext)Activator.CreateInstance(model.ContextType, builder.Options)!;
    }

    /// <summary>
    /// L'expression devient le retour d'une methode ou une variable porte le nom de la base.
    /// #line recale les diagnostics sur le texte de l'onglet ; le ";" final tolere un ";" saisi.
    /// Sans modele (pas de connexion), le source reste valide pour l'autocompletion C#.
    /// </summary>
    /// <returns>Le source et la position du texte de la requete dans ce source.</returns>
    internal static (string Source, int QueryStart) BuildQuerySource(EfGeneratedModel? model, string query)
    {
        var code = new StringBuilder();
        code.AppendLine("using System;");
        code.AppendLine("using System.Collections.Generic;");
        code.AppendLine("using System.Linq;");
        code.AppendLine("using Microsoft.EntityFrameworkCore;");
        if (model is not null)
            code.AppendLine($"using {model.Namespace};");
        code.AppendLine();
        code.AppendLine($"namespace {QueryNamespace};");
        code.AppendLine();
        code.AppendLine($"public static class {QueryClass}");
        code.AppendLine("{");
        code.AppendLine($"    public static object {QueryMethod}(DbContext __context)");
        code.AppendLine("    {");
        if (model is not null)
            code.AppendLine($"        var {model.ContextVariableName} = ({model.ContextClassName})__context;");

        code.AppendLine("        return");
        code.AppendLine($"#line 1 \"{QueryFileName}\"");

        var queryStart = code.Length;
        code.AppendLine(query);
        code.AppendLine("#line default");
        code.AppendLine("        ;");
        code.AppendLine("    }");
        code.AppendLine("}");
        return (code.ToString(), queryStart);
    }

    private static byte[]? Emit(string assemblyName, string source, IEnumerable<MetadataReference> references, out IReadOnlyList<LinqDiagnostic> errors)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable));

        using var image = new MemoryStream();
        var result = compilation.Emit(image);

        errors = result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(ToDiagnostic)
            .ToArray();

        return result.Success ? image.ToArray() : null;
    }

    private static LinqDiagnostic ToDiagnostic(Diagnostic diagnostic)
    {
        var position = diagnostic.Location.GetMappedLineSpan().StartLinePosition;
        return new LinqDiagnostic(position.Line + 1, position.Character + 1, diagnostic.Id, diagnostic.GetMessage(System.Globalization.CultureInfo.CurrentUICulture));
    }

    private static IReadOnlyList<MetadataReference> LoadPlatformReferences()
    {
        var paths = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?.Split(Path.PathSeparator)
            // Hote sans TPA : les assemblies deja chargees font l'affaire.
            ?? AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location)).Select(a => a.Location).ToArray();

        // EF n'est peut-etre pas encore charge : on force ses assemblies dans la liste.
        string[] required = [typeof(DbContext).Assembly.Location, typeof(SqlServerDbContextOptionsExtensions).Assembly.Location, typeof(RelationalQueryableExtensions).Assembly.Location];

        return paths.Concat(required)
                    .Where(path => !string.IsNullOrEmpty(path) && ReferencedAssemblyPrefixes.Any(p => Path.GetFileName(path).StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                    .ToArray();
    }

    /// <summary>Contexte d'une requete : le modele est servi tel quel, le reste vient du contexte par defaut.</summary>
    private sealed class QueryLoadContext(Assembly model) : AssemblyLoadContext(isCollectible: false)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
            => AssemblyName.ReferenceMatchesDefinition(assemblyName, model.GetName()) ? model : null;
    }
}
