namespace HelpMotivateMe.Core.Interfaces;

/// <summary>
///     A read-only query interface that wraps IQueryable with AsNoTracking() for query-only scenarios.
///     Use this for read operations where change tracking is not needed.
/// </summary>
public interface IQueryInterface<T> : IQueryable<T> where T : class
{
}