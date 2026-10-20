using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Runtime.Loader;

namespace SqlToolManagerMaui.Languages.Linq;

/// <summary>
/// Compilation Roslyn en memoire : le DbContext genere (une fois par schema), puis chaque
/// requete LINQ, enveloppee dans une methode qui recoit le contexte sous le nom de la base.
/// </summary>
public interface ILinqCompiler
{
    /// <summary>Compile et charge le modele genere ; leve <see cref="LinqCompilationException"/> en cas d'echec.</summary>
    LinqModelAssembly CompileModel(EfGeneratedModel model);

    /// <summary>Compile une expression LINQ ; les erreurs sont rapportees, pas levees.</summary>
    LinqCompiledQuery CompileQuery(LinqModelAssembly model, string query);

    /// <summary>Contexte EF du modele sur une chaine de connexion SQL Server.</summary>
    DbContext CreateContext(LinqModelAssembly model, string connectionString);
}

/// <summary>Modele compile et charge, reutilisable par toutes les requetes sur ce schema.</summary>
public sealed class LinqModelAssembly
{
    public required EfGeneratedModel Model { get; init; }

    public required Assembly Assembly { get; init; }

    public required Type ContextType { get; init; }

    /// <summary>Reference Roslyn vers l'image compilee, pour compiler les requetes contre elle.</summary>
    public required MetadataReference Reference { get; init; }
}

/// <summary>
/// Requete compilee : un point d'entree executable, ou les erreurs de compilation.
/// <see cref="Dispose"/> decharge son assembly : a appeler une fois l'execution terminee.
/// </summary>
public sealed class LinqCompiledQuery : IDisposable
{
    private int _isDisposed;

    /// <summary>Contexte collectable de l'assembly de la requete ; null en cas d'erreur de compilation.</summary>
    public AssemblyLoadContext? LoadContext { get; init; }

    /// <summary>Execute la requete ; chaque appel a Dump(valeur, titre) est transmis au second parametre.</summary>
    public Func<DbContext, Action<object?, string?>?, object?>? Execute { get; init; }

    /// <summary>Execution sans recuperation des Dump().</summary>
    public Func<DbContext, object?>? Run => Execute is { } execute ? context => execute(context, null) : null;

    /// <summary>Faux pour des instructions sans return : la valeur rendue (null) n'est pas un resultat.</summary>
    public bool ReturnsValue { get; init; }

    public IReadOnlyList<LinqDiagnostic> Errors { get; init; } = [];

    public bool Success => Execute is not null;

    /// <summary>L'assembly est libere par le GC des que plus rien ne reference ses types (resultats deja convertis en texte).</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
            LoadContext?.Unload();
    }
}

/// <param name="Line">Ligne dans le texte de la requete (base 1).</param>
/// <param name="Column">Colonne dans le texte de la requete (base 1).</param>
public sealed record LinqDiagnostic(int Line, int Column, string Id, string Message)
{
    public override string ToString() => $"Ligne {Line}, colonne {Column} : {Id} {Message}";
}

/// <summary>Le code genere ne compile pas : bug du generateur ou cas de schema non prevu.</summary>
public sealed class LinqCompilationException : Exception
{
    public LinqCompilationException(string message, IReadOnlyList<LinqDiagnostic> errors)
        : base(message)
    {
        Errors = errors;
    }

    public IReadOnlyList<LinqDiagnostic> Errors { get; }
}
