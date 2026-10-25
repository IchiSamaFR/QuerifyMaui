namespace SqlToolManagerMaui.Languages.Linq.Compilation;

/// <summary>Le code genere ne compile pas : bug du generateur ou cas de schema non prevu.</summary>
public sealed class LinqCompilationException : Exception
{
    public LinqCompilationException(string message, IReadOnlyList<string> errors)
        : base(message)
    {
        Errors = errors;
    }

    /// <summary>Messages Roslyn, positions dans le source genere.</summary>
    public IReadOnlyList<string> Errors { get; }
}
