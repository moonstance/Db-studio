using DbStudio.Shared.ScriptTemplates;
using System.Collections.ObjectModel;
using System.Security;

namespace DbStudio.Shared;

public class DataSource {
  public string Name { get; set; }
  public string Url { get; set; }
  public string? CertificatePath { get; set; }

  public ObservableCollection<Database> Databases { get; set; } = new ObservableCollection<Database>();
  public string? CertificatePassword { get;  set; }
  public string? DomainAssemblyPath { get; set; }
  public ScriptTemplate? ScriptTemplate { get; set; }
  public DbType DbType { get; set; }

  public static DataSource CreateDefault() {
    return new DataSource() {
      Url = "http://localhost:8080",
      Name = "Local dev"
    };
  }
}
