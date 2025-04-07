using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Queries;
using Raven.Client.Documents.Session;
using Raven.Client.ServerWide.Operations;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;

namespace DbStudio.Shared;
public static class RavenHelper {
  //static IDocumentStore store;

  static Dictionary<string, IDocumentStore> _stores = new Dictionary<string, IDocumentStore>();


  public static IDocumentStore GetDocumentStore(RavenStore ravenStore) {
    string key = $"{ravenStore.Url}-{ravenStore.Database.Name}";

    if (_stores.ContainsKey(key)) { 
      return _stores[key];
    } else {
      var store = new DocumentStore {
        Urls = new[] { ravenStore.Url },
        Database = ravenStore.Database.Name,
      };

      if (!string.IsNullOrEmpty(ravenStore.CertificatePath)) {
        var cert = new X509Certificate2(ravenStore.CertificatePath, PasswordProtector.DecryptPassword(ravenStore.Password));
        //var cert = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificateFromFile(_ravenStore.CertificatePath);
        store.Certificate = cert;
      }

      store.Initialize();

      _stores[key] = store;

      return store;
    }
  }


  public static IDocumentSession GetSession(RavenStore ravenStore) {

    var store = GetDocumentStore(ravenStore);
    return store.OpenSession();
  }

  public static string[] GetDatabases(RavenStore ravenStore) {
    var operation = new GetDatabaseNamesOperation(0, 25);
    var store = GetDocumentStore(ravenStore);
    string[] databaseNames = store.Maintenance.Server.Send(operation);

    return databaseNames;
  }


  public static QueryResult<T> ToListWithStats<T>(this IRavenQueryable<T> queryable) {
    var r = new QueryResult<T>();

    queryable.Statistics(out var stats);

    r.List = queryable.ToList();
    r.QueryStatistics = stats;
    r.Raw = queryable.ToString();

    return r;
  }

  public static async Task<QueryResult<T>> ToListAsyncWithStats<T>(this IRavenQueryable<T> queryable) {
    var r = new QueryResult<T>();

    queryable.Statistics(out var stats);

    r.List = await queryable.ToListAsync();
    r.QueryStatistics = stats;
    r.Raw = queryable.ToString();

    return r;
  }

}
