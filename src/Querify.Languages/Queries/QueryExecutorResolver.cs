using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Databases.Common;

namespace Querify.Languages.Queries;

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
