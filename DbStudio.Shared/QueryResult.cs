using Raven.Client.Documents.Session;
using System.Collections;

namespace DbStudio.Shared;

public class QueryResult<T> : IQueryResult {
  public List<T> List { get; set; }
  public QueryStatistics QueryStatistics { get; set; }
  public string? Raw { get; set; }

  public IEnumerable AsEnumerable() => List;
}
