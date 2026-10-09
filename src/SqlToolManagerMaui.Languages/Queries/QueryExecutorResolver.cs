using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Languages.Commons.Queries;

namespace SqlToolManagerMaui.Languages.Queries;

/// <inheritdoc cref="IQueryExecutorResolver"/>
[AutoRegister(As = typeof(IQueryExecutorResolver))]
public sealed class QueryExecutorResolver : IQueryExecutorResolver
{
    private readonly Dictionary<SyntaxLanguage, IQueryExecutor> _executors;

    public QueryExecutorResolver(IEnumerable<IQueryExecutor> executors)
    {
        ArgumentNullException.ThrowIfNull(executors);
        _executors = executors.ToDictionary(e => e.Language);
    }

    public IQueryExecutor Resolve(SyntaxLanguage language)
        => _executors.TryGetValue(language, out var executor)
            ? executor
            : throw new NotSupportedException($"Aucun exécuteur enregistré pour le langage {language}.");
}
