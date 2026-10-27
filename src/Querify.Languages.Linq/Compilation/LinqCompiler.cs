using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Querify.Core.Ioc;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;
using Quill.Code.CSharp;
using Quill.Code.CSharp.Compilation;
using Quill.Code.CSharp.Workspaces;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Querify.Languages.Linq.Compilation;

/// <inheritdoc cref="ILinqCompiler"/>
[AutoRegister(As = typeof(ILinqCompiler))]
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "App non publiee en trimmed : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "App non publiee en trimmed : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "App non publiee en trimmed : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "App non publiee en AOT : compilation et reflexion assumees.")]
public sealed class LinqCompiler : ILinqCompiler
{
    private const string QueryNamespace = "Querify.UserQueries";
    private const string QueryClass = "UserQuery";
    private const string QueryMethod = "Run";
    private const string DumpClass = "QueryExtensions";
    private const string ConsoleClass = "Console";

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

    private readonly ILinqLanguageServices _services;

    public LinqCompiler(ILinqLanguageServices services)
    {
        _services = services;
    }

    public async Task<LinqModelAssembly> CompileModelAsync(EfGeneratedModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        // Projet a part, sans editeur : le modele est compile seul, une fois par schema.
        using var service = new CSharpLanguageService($"EfModel_{model.ContextVariableName}", () => new CSharpProjectOptions
        {
            References = PlatformReferences,
            CompilationOptions = CompilationOptions,
            ParseOptions = ParseOptions,
        });

        // Non collectable : le modele vit tant que l'application, et EF garde ses types en cache.
        var compiled = await service.CompileAsync(model.Source, new CSharpCompileOptions
        {
            AssemblyName = $"EfModel_{Guid.NewGuid():N}",
            Collectible = false,
        }, cancellationToken);

        if (compiled.Assembly is not { } assembly)
            throw new LinqCompilationException($"Le modele genere pour {model.ContextVariableName} ne compile pas.", compiled.Errors.Select(e => e.Describe(model.Source)).ToArray());

        return new LinqModelAssembly
        {
            Model = model,
            Assembly = assembly,
            ContextType = assembly.GetType($"{model.Namespace}.{model.ContextClassName}", throwOnError: true)!,
            Reference = compiled.Reference!
        };
    }

    public async Task<LinqCompiledQuery> CompileQueryAsync(LinqModelAssembly model, string query, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(query);

        parameters ??= [];

        // Projet meme de l'editeur (references, contexte genere) : ce qui est souligne est ce qui echoue.
        var compiled = await _services.Get(model, parameters).CompileAsync(query, new CSharpCompileOptions
        {
            AssemblyName = $"LinqQuery_{Guid.NewGuid():N}",

            // Le modele est servi tel quel (EF garde ses types en cache), le reste vient du runtime.
            ResolveAssembly = name => AssemblyName.ReferenceMatchesDefinition(name, model.Assembly.GetName()) ? model.Assembly : null,
        }, cancellationToken);

        if (compiled.Assembly is not { } assembly)
        {
            compiled.Dispose();
            return new LinqCompiledQuery { Errors = compiled.Errors };
        }

        var method = assembly.GetType($"{QueryNamespace}.{QueryClass}", throwOnError: true)!.GetMethod(QueryMethod)!;
        var run = method.CreateDelegate<Func<DbContext, Action<object?, string?>?, Action<string?>?, object?[], object?>>();
        var values = parameters.Select(p => p.Value).ToArray();

        return new LinqCompiledQuery
        {
            Execute = (context, dump, write) => run(context, dump, write, values),
            ReturnsValue = ReturnsValue(query),
            Compilation = compiled,
        };
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
    /// Dump() facon LINQPad et Console de la requete, document fixe du projet (<see cref="QueryRuntimeSource"/>).
    /// Dump affiche une valeur dans son propre onglet et la rend telle quelle. Console masque System.Console dans le
    /// namespace de la requete : la sortie va dans l'onglet Messages, sans Console.SetOut global (plusieurs requetes
    /// peuvent tourner en meme temps). Les collecteurs sont statiques : chaque requete est compilee dans sa propre assembly.
    /// </summary>
    internal static string QueryRuntimeSource { get; } = $$"""
        namespace {{QueryNamespace}};

        public static class {{DumpClass}}
        {
            internal static Action<object, string> __sink;

            public static T Dump<T>(this T value, string title = null)
            {
                __sink?.Invoke(value, title);
                return value;
            }
        }

        public static class {{ConsoleClass}}
        {
            internal static Action<string> __sink;

            public static void Write(string value) => __sink?.Invoke(value);
            public static void Write(object value) => __sink?.Invoke(Convert.ToString(value));
            public static void Write(string format, params object[] args) => __sink?.Invoke(string.Format(format, args));
            public static void WriteLine() => __sink?.Invoke(Environment.NewLine);
            public static void WriteLine(string value) => __sink?.Invoke(value + Environment.NewLine);
            public static void WriteLine(object value) => __sink?.Invoke(Convert.ToString(value) + Environment.NewLine);
            public static void WriteLine(string format, params object[] args) => __sink?.Invoke(string.Format(format, args) + Environment.NewLine);
        }
        """;

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
    /// Usings et Dump/Console sont des documents fixes du projet (<see cref="BuildGlobalUsings"/>, <see cref="QueryRuntimeSource"/>).
    /// </summary>
    /// <returns>Le source et la position du texte de la requete dans ce source.</returns>
    internal static (string Source, int QueryStart) BuildQuerySource(EfGeneratedModel? model, string query, IReadOnlyList<LinqParameter>? parameters = null)
    {
        var code = new StringBuilder();
        code.AppendLine($"namespace {QueryNamespace};");
        code.AppendLine();
        code.AppendLine($"public static class {QueryClass}");
        code.AppendLine("{");
        code.AppendLine($"    public static object {QueryMethod}(DbContext __context, Action<object, string> __dump, Action<string> __write, object[] __args)");
        code.AppendLine("    {");
        code.AppendLine($"        {DumpClass}.__sink = __dump;");
        code.AppendLine($"        {ConsoleClass}.__sink = __write;");
        if (model is not null)
            code.AppendLine($"        var {model.ContextVariableName} = ({model.ContextClassName})__context;");

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
