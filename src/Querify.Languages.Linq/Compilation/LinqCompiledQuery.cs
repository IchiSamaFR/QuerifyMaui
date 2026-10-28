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
    /// <summary>Assembly de la requete chargee par Quill (contexte collectable) ; null en cas d'erreur de compilation.</summary>
    public CSharpCompilationResult? Compilation { get; init; }

    /// <summary>Execute la requete sur le contexte ; Dump(valeur, titre) est transmis au second parametre (Log : voir la compilation).</summary>
    public Func<DbContext, Action<object?, string?>?, object?>? Execute { get; init; }

    /// <summary>Execution sans recuperation des Dump().</summary>
    public Func<DbContext, object?>? Run => Execute is { } execute ? context => execute(context, null) : null;

    /// <summary>Faux pour des instructions sans return : la valeur rendue (null) n'est pas un resultat.</summary>
    public bool ReturnsValue { get; init; }

    /// <summary>Positions dans le texte de la requete, comme les soulignements de l'editeur.</summary>
    public IReadOnlyList<CodeDiagnostic> Errors { get; init; } = [];

    public bool Success => Execute is not null;

    /// <summary>L'assembly est libere par le GC des que plus rien ne reference ses types (resultats deja convertis en texte).</summary>
    public void Dispose() => Compilation?.Dispose();
}
