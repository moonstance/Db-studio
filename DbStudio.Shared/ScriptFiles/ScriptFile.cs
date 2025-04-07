namespace DbStudio.Shared.ScriptFiles;
public class ScriptFile {
  public string Name { get; set; } = "";
  public string FullPath { get; set; } = "";
  public DateTime LastModified { get; set; }
  public DbType DbType { get; set; }
}

