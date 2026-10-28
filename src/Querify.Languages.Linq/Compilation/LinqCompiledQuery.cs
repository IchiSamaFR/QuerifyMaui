using Microsoft.EntityFrameworkCore;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Compilation;

namespace Querify.Languages.Linq.Compilation;

/// <summary>
/// Requete compilee : un point d'entree executable, ou les erreurs de compilation. Executee par
/// <see cref="CSharpCompilationResult.RunAsync{T}"/> ; <see cref="Dispose"/> decharge son assembly.
/// </summary>
public sealed class LinqCompiledQuery : IDisposable
{
    private readonly LinqModelAssembly? _model;
    private readonly Func<object?>? _entryPoint;

    internal LinqCompiledQuery(IReadOnlyList<CodeDiagnostic> errors)
    {
        Errors = errors;
    }

    internal LinqCompiledQuery(LinqModelAssembly model, CSharpCompilationResult compilation, Func<object?> entryPoint, bool returnsValue)
    {
        _model = model;
        _entryPoint = entryPoint;
        Compilation = compilation;
        ReturnsValue = returnsValue;
    }

    /// <summary>Assembly de la requete chargee par Quill (contexte collectable) ; null en cas d'erreur de compilation.</summary>
    public CSharpCompilationResult? Compilation { get; }

    /// <summary>Contexte sur lequel la requete s'execute ; a fixer avant <see cref="Run"/>.</summary>
    public DbContext? Context { get; set; }

    /// <summary>Recoit chaque Dump(valeur, titre) de la requete ; null : ignores. Le Log est branche a la compilation.</summary>
    public Action<object?, string?>? Dump { get; set; }

    /// <summary>Faux pour des instructions sans return : la valeur rendue (null) n'est pas un resultat.</summary>
    public bool ReturnsValue { get; }

    /// <summary>Positions dans le texte de la requete, comme les soulignements de l'editeur.</summary>
    public IReadOnlyList<CodeDiagnostic> Errors { get; } = [];

    public bool Success => _entryPoint is not null;

    /// <summary>
    /// Execute la requete sur <see cref="Context"/>. Contexte et Dump sont fixes dans le modele pour le flux courant
    /// (AsyncLocal) : d'autres requetes peuvent tourner en meme temps sur le meme modele.
    /// </summary>
    /// <exception cref="InvalidOperationException">Requete en erreur, ou <see cref="Context"/> non fixe.</exception>
    public object? Run()
    {
        if (_model is null || _entryPoint is null)
            throw new InvalidOperationException("La requete ne compile pas : rien a executer.");

        var context = Context ?? throw new InvalidOperationException("Le contexte de la requete doit etre fixe avant son execution.");

        _model.ContextProperty.SetValue(null, context);
        _model.DumpSinkProperty.SetValue(null, Dump);
        return _entryPoint();
    }

    /// <summary>L'assembly est libere par le GC des que plus rien ne reference ses types (resultats deja convertis en texte).</summary>
    public void Dispose() => Compilation?.Dispose();
}
