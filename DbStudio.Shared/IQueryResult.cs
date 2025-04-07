using Raven.Client.Documents.Session;
using System.Collections;

namespace DbStudio.Shared;

public interface IQueryResult {
  IEnumerable AsEnumerable();
  QueryStatistics QueryStatistics { get; }

  public string? Raw { get; set; }

}
