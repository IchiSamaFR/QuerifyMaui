using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Querify.Core.Ioc;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Analysis;
using Quill.Code.CSharp.Workspaces;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
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

    private readonly ILinqWorkspace _workspace;

    public LinqCompiler(ILinqWorkspace workspace)
    {
        _workspace = workspace;
    }

    public LinqModelAssembly CompileModel(EfGeneratedModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var compilation = CSharpCompilation.Create($"EfModel_{Guid.NewGuid():N}", [CSharpSyntaxTree.ParseText(model.Source, ParseOptions)], PlatformReferences, CompilationOptions);
        var image = Emit(compilation, out var errors);
        if (image is null)
            throw new LinqCompilationException($"Le modele genere pour {model.ContextVariableName} ne compile pas.", errors.Select(e => e.ToString()).ToArray());

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

    public async Task<LinqCompiledQuery> CompileQueryAsync(LinqModelAssembly model, string query, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(query);

        parameters ??= [];

        // Le projet Roslyn de l'editeur a deja lie les references : la compilation repart de lui.
        var linq = _workspace.GetDocument(model, query, parameters);
        var compilation = await linq.Document.Project.GetCompilationAsync(cancellationToken)
            ?? throw new InvalidOperationException("Roslyn n'a pas produit de compilation pour la requete.");

        // Nom unique : chaque requete est chargee dans son propre contexte collectable.
        var image = Emit(compilation.WithAssemblyName($"LinqQuery_{Guid.NewGuid():N}"), out var errors, cancellationToken);
        if (image is null)
            return new LinqCompiledQuery { Errors = await QueryErrorsAsync(linq, errors, cancellationToken) };

        var loadContext = new QueryLoadContext(model.Assembly);
        var assembly = loadContext.LoadFromStream(new MemoryStream(image));
        var method = assembly.GetType($"{QueryNamespace}.{QueryClass}", throwOnError: true)!.GetMethod(QueryMethod)!;
        var run = method.CreateDelegate<Func<DbContext, Action<object?, string?>?, Action<string?>?, object?[], object?>>();
        var values = parameters.Select(p => p.Value).ToArray();

        return new LinqCompiledQuery
        {
            Execute = (context, dump, write) => run(context, dump, write, values),
            ReturnsValue = ReturnsValue(query),
            LoadContext = loadContext,
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
    /// La requete devient le corps d'une methode ou une variable porte le nom de la base :
    /// une expression seule est retournee telle quelle (le ";" final est tolere), des
    /// instructions avec "return" forment le corps lui-meme (voir <see cref="IsStatementBlock"/>).
    /// #line recale les diagnostics sur le texte de l'onglet.
    /// Sans modele (pas de connexion), le source reste valide pour l'autocompletion C#.
    /// </summary>
    /// <returns>Le source et la position du texte de la requete dans ce source.</returns>
    internal static (string Source, int QueryStart) BuildQuerySource(EfGeneratedModel? model, string query, IReadOnlyList<LinqParameter>? parameters = null)
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
        // Dump() facon LINQPad : affiche une valeur dans son propre onglet et la rend telle quelle.
        // Le collecteur est statique : chaque requete est compilee dans sa propre assembly.
        code.AppendLine($"public static class {DumpClass}");
        code.AppendLine("{");
        code.AppendLine("    internal static Action<object, string> __sink;");
        code.AppendLine();
        code.AppendLine("    public static T Dump<T>(this T value, string title = null)");
        code.AppendLine("    {");
        code.AppendLine("        __sink?.Invoke(value, title);");
        code.AppendLine("        return value;");
        code.AppendLine("    }");
        code.AppendLine("}");
        code.AppendLine();
        // Masque System.Console dans le namespace de la requete : la sortie va dans l'onglet Messages,
        // sans Console.SetOut global (plusieurs requetes peuvent tourner en meme temps).
        code.AppendLine($"public static class {ConsoleClass}");
        code.AppendLine("{");
        code.AppendLine("    internal static Action<string> __sink;");
        code.AppendLine();
        code.AppendLine("    public static void Write(string value) => __sink?.Invoke(value);");
        code.AppendLine("    public static void Write(object value) => __sink?.Invoke(Convert.ToString(value));");
        code.AppendLine("    public static void Write(string format, params object[] args) => __sink?.Invoke(string.Format(format, args));");
        code.AppendLine("    public static void WriteLine() => __sink?.Invoke(Environment.NewLine);");
        code.AppendLine("    public static void WriteLine(string value) => __sink?.Invoke(value + Environment.NewLine);");
        code.AppendLine("    public static void WriteLine(object value) => __sink?.Invoke(Convert.ToString(value) + Environment.NewLine);");
        code.AppendLine("    public static void WriteLine(string format, params object[] args) => __sink?.Invoke(string.Format(format, args) + Environment.NewLine);");
        code.AppendLine("}");
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

    private static byte[]? Emit(Microsoft.CodeAnalysis.Compilation compilation, out IReadOnlyList<Diagnostic> errors, CancellationToken cancellationToken = default)
    {
        using var image = new MemoryStream();
        var result = compilation.Emit(image, cancellationToken: cancellationToken);

        errors = result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        return result.Success ? image.ToArray() : null;
    }

    /// <summary>Erreurs ramenees au texte de l'onglet, comme les soulignements de l'editeur.</summary>
    private static async Task<IReadOnlyList<CodeDiagnostic>> QueryErrorsAsync(CSharpDocument linq, IReadOnlyList<Diagnostic> errors, CancellationToken cancellationToken)
    {
        var mapped = (await CSharpDiagnosticMapper.MapAsync(linq, errors, cancellationToken)).Where(e => e.IsError).ToArray();

        // Erreur dans le code genere autour de la requete : sans position utile, mais jamais silencieuse.
        return mapped.Length > 0
            ? mapped
            : [.. errors.Take(1).Select(e => new CodeDiagnostic(0, 0, CodeDiagnosticSeverity.Error, e.GetMessage(CultureInfo.CurrentUICulture), e.Id))];
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

    /// <summary>
    /// Contexte d'une requete : le modele est servi tel quel, le reste vient du contexte par defaut.
    /// Collectable : sinon chaque execution garde son assembly en memoire jusqu'a la fermeture de l'application.
    /// </summary>
    private sealed class QueryLoadContext(Assembly model) : AssemblyLoadContext(isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
            => AssemblyName.ReferenceMatchesDefinition(assemblyName, model.GetName()) ? model : null;
    }
}
