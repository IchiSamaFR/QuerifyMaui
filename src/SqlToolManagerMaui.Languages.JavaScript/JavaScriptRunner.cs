using Acornima.Ast;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Jint.Runtime.Interop;
using SqlToolManagerMaui.Core.Enums.Queries;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Databases.Models.Queries;
using SqlToolManagerMaui.Databases.Queries;
using SqlToolManagerMaui.Languages.Commons.Queries;
using System.Diagnostics;

namespace SqlToolManagerMaui.Languages.JavaScript;

/// <inheritdoc cref="IJavaScriptRunner"/>
[AutoRegister(As = typeof(IJavaScriptRunner))]
public sealed class JavaScriptRunner : IJavaScriptRunner
{
    private const int MaxRows = QueryExecutionLimits.MaxRowsPerResultSet;

    // Non enumerable : dump n'apparait pas dans les colonnes ; "use strict" garde les primitives telles quelles.
    private const string DumpInstaller = """
        (function (sink) {
            Object.defineProperty(Object.prototype, "dump", {
                value: function (title) { "use strict"; return sink(this, title); },
                writable: true,
                configurable: true,
                enumerable: false
            });
        })
        """;

    private readonly IQueryReaderResolver _readers;
    private readonly IScriptExecutionSettings _settings;

    public JavaScriptRunner(IQueryReaderResolver readers, IScriptExecutionSettings settings)
    {
        _readers = readers;
        _settings = settings;
    }

    public async Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string script, Func<string, string>? transform, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(script);

        List<QueryResultSet> resultSets = [];
        List<QueryMessage> errors = [];
        var wasCancelled = false;
        var console = new JavaScriptConsole();
        var timeout = _settings.ScriptTimeout;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Pas de WaitAsync : les contraintes Jint interrompent vraiment le script (boucle infinie, annulation).
            var error = await Task.Run(() => Run(connection, script, transform, console, resultSets, timeout, cancellationToken), CancellationToken.None);
            if (error is not null)
                errors.Add(ErrorMessage(error));
        }
        catch (Exception ex) when (ex is ExecutionCanceledException or OperationCanceledException)
        {
            wasCancelled = true;
        }
        catch (ScriptSyntaxException ex)
        {
            errors.Add(ErrorMessage(Locate(ex.Line, ex.Column, $"SyntaxError : {ex.Message}")));
        }
        catch (TimeoutException)
        {
            errors.Add(ErrorMessage($"Délai d'exécution dépassé ({FormatTimeout(timeout)}) : script interrompu. Le délai se règle dans Requête > Délai des scripts."));
        }
        catch (StatementsCountOverflowException)
        {
            errors.Add(ErrorMessage($"Script interrompu après {JavaScriptLimits.MaxStatements:N0} instructions (boucle infinie ?)."));
        }
        catch (MemoryLimitExceededException)
        {
            errors.Add(ErrorMessage($"Script interrompu : plus de {JavaScriptLimits.MaxAllocatedBytes / (1024 * 1024):N0} Mo alloués."));
        }
        catch (RecursionDepthOverflowException)
        {
            errors.Add(ErrorMessage($"RangeError : plus de {JavaScriptLimits.MaxCallDepth:N0} appels imbriqués."));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Le script est du code de l'utilisateur : toute exception est un resultat.
            errors.Add(ErrorMessage($"{ex.GetType().Name} : {ex.Message}"));
        }

        stopwatch.Stop();

        // La console a ete ecrite pendant l'execution : avant les resultats et l'eventuelle erreur.
        List<QueryMessage> messages = [.. console.ToMessages()];
        foreach (var set in resultSets)
        {
            var prefix = resultSets.Count > 1 ? $"{set.Title} : " : string.Empty;
            messages.Add(new QueryMessage($"{prefix}({set.Rows.Count} ligne(s))"));
        }

        if (resultSets.Count == 0 && errors.Count == 0 && !wasCancelled)
            messages.Add(new QueryMessage("Aucun résultat : terminez par return ou affichez une valeur avec .dump()."));

        messages.AddRange(errors);

        if (wasCancelled)
            messages.Add(ErrorMessage("Requête annulée par l'utilisateur."));

        messages.Add(new QueryMessage($"Heure de fin : {DateTime.Now:yyyy-MM-dd HH:mm:ss} (durée {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff})"));

        return new QueryExecutionResult(resultSets, messages, stopwatch.Elapsed, errors.Count > 0, wasCancelled);
    }

    /// <returns>L'erreur JavaScript decrite, ou null si le script s'est termine normalement.</returns>
    private string? Run(DatabaseConnectionInfo connection, string script, Func<string, string>? transform, JavaScriptConsole console, List<QueryResultSet> outputs, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var prepared = Prepare(transform is null ? script : transform(script));

        using var engine = new Engine(options =>
        {
            options.TimeoutInterval(timeout);
            options.MaxStatements(JavaScriptLimits.MaxStatements);
            options.LimitMemory(JavaScriptLimits.MaxAllocatedBytes);
            options.LimitRecursion(JavaScriptLimits.MaxCallDepth);
            options.CancellationToken(cancellationToken);

            // Garde de pile : une recursion profonde finit en RangeError au lieu de fermer l'application.
            options.Constraints.MaxExecutionStackCount = JavaScriptLimits.MaxCallDepth;

            // Dates echangees avec la base en heure locale, comme les colonnes datetime.
            options.Interop.DateTimeKind = DateTimeKind.Local;
        });

        JsValue? lastDumped = null;
        var dump = new ClrFunction(engine, "dump", (_, arguments) =>
        {
            var value = arguments.Length > 0 ? arguments[0] : JsValue.Undefined;
            var title = arguments.Length > 1 && arguments[1].IsString() ? arguments[1].AsString() : null;

            // Capture immediate : dump() montre la valeur a cet instant, meme si le script la modifie ensuite.
            outputs.Add(JavaScriptResultConverter.ToResultSet(engine, value, outputs.Count + 1, title, MaxRows, cancellationToken));
            lastDumped = value;
            return value;
        });

        engine.Call(engine.Evaluate(DumpInstaller), dump);
        engine.SetValue("console", (JsValue)console.Create(engine));
        engine.SetValue("db", (JsValue)new JavaScriptDatabase(engine, connection, _readers, console, cancellationToken).Create());

        try
        {
            var value = engine.Evaluate(prepared);

            // return x.dump() : la valeur est deja affichee, pas de second onglet identique.
            if (ReturnsValue(prepared.Program!) && !value.IsUndefined() && !Equals(value, lastDumped))
                outputs.Add(JavaScriptResultConverter.ToResultSet(engine, value, outputs.Count + 1, title: null, MaxRows, cancellationToken));

            return null;
        }
        catch (JavaScriptException ex)
        {
            // Decrite ici : l'objet erreur appartient au moteur, libere en sortie de methode.
            return Describe(ex);
        }
    }

    private static Prepared<Script> Prepare(string source)
    {
        try
        {
            return Engine.PrepareScript(source, "script");
        }
        catch (ScriptPreparationException ex) when (ex.InnerException is Acornima.ParseErrorException parse)
        {
            throw new ScriptSyntaxException(parse.Description, parse.LineNumber, parse.Column + 1);
        }
    }

    /// <summary>
    /// Comme une expression seule en LINQ : la valeur finale compte si le script se termine par une
    /// expression ou contient un return. Sinon (boucle, declaration...) elle n'est pas un resultat.
    /// </summary>
    internal static bool ReturnsValue(Script program)
    {
        var body = program.Body;
        return (body.Count > 0 && body[body.Count - 1] is ExpressionStatement and not Directive) || ContainsReturn(program);
    }

    private static bool ContainsReturn(Node node)
    {
        foreach (var child in node.ChildNodes)
        {
            if (child is ReturnStatement)
                return true;

            // Un return dans une fonction ne termine pas le script.
            if (child is not IFunction && ContainsReturn(child))
                return true;
        }

        return false;
    }

    private static string Describe(JavaScriptException ex)
    {
        var name = ex.Error.IsObject() ? ex.Error.AsObject().Get("name") : JsValue.Undefined;
        var text = name.IsString() ? $"{name.AsString()} : {ex.Message}" : ex.Message;
        var start = ex.Location.Start;
        return Locate(start.Line, start.Column + 1, text);
    }

    private static string Locate(int line, int column, string text)
        => line > 0 ? $"Ligne {line}, colonne {column} : {text}" : text;

    private static string FormatTimeout(TimeSpan timeout)
        => timeout.TotalMinutes >= 1 ? $"{timeout.TotalMinutes:0.#} min" : $"{timeout.TotalSeconds:0} s";

    private static QueryMessage ErrorMessage(string text) => new(text, QueryMessageKind.Error);
}
