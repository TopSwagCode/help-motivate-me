using System.Collections;
using System.Linq.Expressions;
using HelpMotivateMe.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HelpMotivateMe.Infrastructure.Data;

/// <summary>
///     A read-only query interface that wraps IQueryable with AsNoTracking() for query-only scenarios.
///     Use this when querying data without the need for change tracking, improving performance.
/// </summary>
public class QueryInterface<T> : IQueryInterface<T> where T : class
{
    private readonly IQueryable<T> _queryable;

    public QueryInterface(AppDbContext db)
    {
        _queryable = db.Set<T>().AsNoTracking();
    }

    public Type ElementType => _queryable.ElementType;
    public Expression Expression => _queryable.Expression;
    public IQueryProvider Provider => _queryable.Provider;

    public IEnumerator<T> GetEnumerator()
    {
        return _queryable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _queryable.GetEnumerator();
    }
}