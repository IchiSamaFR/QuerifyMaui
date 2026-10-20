using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Databases.Common;
using SqlToolManagerMaui.Languages.Commons.Queries;

namespace SqlToolManagerMaui.Languages.Queries;

/// <inheritdoc cref="IQueryExecutorResolver"/>
[AutoRegister(As = typeof(IQueryExecutorResolver))]
public sealed class QueryExecutorResolver : IQueryExecutorResolver
{
    private readonly KeyedServiceMap<SyntaxLanguage, IQueryExecutor> _executors;

    public QueryExecutorResolver(IEnumerable<IQueryExecutor> executors)
    {
        _executors = new(executors, e => e.Language, "exécuteur de langage");
    }

    public IQueryExecutor Resolve(SyntaxLanguage language) => _executors.Resolve(language);
}
