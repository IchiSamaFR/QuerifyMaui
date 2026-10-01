using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace SqlToolManagerMaui.Services.Linq;

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

/// <summary>Requete compilee : un point d'entree executable, ou les erreurs de compilation.</summary>
public sealed class LinqCompiledQuery
{
    public Func<DbContext, object?>? Run { get; init; }

    public IReadOnlyList<LinqDiagnostic> Errors { get; init; } = [];

    public bool Success => Run is not null;
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
