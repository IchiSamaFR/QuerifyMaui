namespace SqlToolManagerMaui.Languages.Linq.Compilation;

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
