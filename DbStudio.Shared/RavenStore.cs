using System.Reflection;
using System.Security;

namespace DbStudio.Shared;

public class RavenStore {
  public string Url { get; set; }
  public Database Database { get; set; }
  public string CertificatePath { get; set; }
  public string? Password { get; internal set; }
  public Assembly? DomainAssembly { get; }

  public RavenStore(DataSource dataSource) {
    this.Url = dataSource.Url;
    this.CertificatePath = dataSource.CertificatePath;
    this.Password = dataSource.CertificatePassword;

    if (!string.IsNullOrWhiteSpace(dataSource.DomainAssemblyPath) && TryLoadAssembly(dataSource.DomainAssemblyPath, out var domainAssembly) && domainAssembly != null ) {
      this.DomainAssembly = domainAssembly;
    }
  }

  public RavenStore(DataSource dataSource, Database database) : this(dataSource) {
    this.Database = database;

    // Override DataSource.DomainAssembly with Database.DomainAssembly if any
    if (!string.IsNullOrWhiteSpace(database.DomainAssemblyPath) && TryLoadAssembly(database.DomainAssemblyPath, out var domainAssembly) && domainAssembly != null) {
      this.DomainAssembly = domainAssembly;
    }
  }

  private bool TryLoadAssembly(string path, out Assembly? domainAssembly) {
    if (File.Exists(path)) {
      try {
        domainAssembly = Assembly.LoadFrom(path);
        return true;
      }
      catch (Exception ex) {
        domainAssembly = null;
        return false;
      }
    }
    domainAssembly = null;
    return false;
  }
}