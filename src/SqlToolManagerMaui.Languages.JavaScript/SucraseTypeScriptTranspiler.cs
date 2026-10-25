using Jint;
using Jint.Native;
using SqlToolManagerMaui.Core.Ioc;

namespace SqlToolManagerMaui.Languages.JavaScript;

/// <summary>Sucrase embarque (Resources/sucrase.js), execute dans un moteur Jint dedie et reutilise.</summary>
[AutoRegister(As = typeof(ITypeScriptTranspiler))]
public sealed class SucraseTypeScriptTranspiler : ITypeScriptTranspiler, IDisposable
{
    private const string ResourceName = "SqlToolManagerMaui.Languages.JavaScript.sucrase.js";

    // Une erreur de Sucrase revient en valeur : sa position (loc) se perdrait en traversant Jint en exception.
    private const string Bootstrap = """
        function __transpile(source) {
            try {
                return { code: sucrase.transform(source, { transforms: ["typescript"], disableESTransforms: true }).code };
            } catch (e) {
                var loc = e && e.loc;
                return {
                    message: String((e && e.message) || e).replace(/\s*\(\d+:\d+\)$/, ""),
                    line: loc ? loc.line : 0,
                    column: loc ? loc.column + 1 : 0
                };
            }
        }
        """;

    // Engine n'est pas thread-safe ; Sucrase (~300 ko) n'est charge qu'une fois.
    private readonly Lock _lock = new();
    private Engine? _engine;

    public string Transpile(string script)
    {
        ArgumentNullException.ThrowIfNull(script);

        lock (_lock)
        {
            _engine ??= CreateEngine();

            var output = _engine.Call("__transpile", new JsString(script)).AsObject();
            var code = output.Get("code");
            if (code.IsString())
                return code.AsString();

            throw new ScriptSyntaxException(output.Get("message").AsString(), (int)output.Get("line").AsNumber(), (int)output.Get("column").AsNumber());
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _engine?.Dispose();
            _engine = null;
        }
    }

    private static Engine CreateEngine()
    {
        var engine = new Engine();
        try
        {
            using var stream = typeof(SucraseTypeScriptTranspiler).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Ressource {ResourceName} introuvable.");
            using var reader = new StreamReader(stream);

            engine.Execute(reader.ReadToEnd(), "sucrase.js");
            engine.Execute(Bootstrap, "transpile.js");
            return engine;
        }
        catch
        {
            engine.Dispose();
            throw;
        }
    }
}
