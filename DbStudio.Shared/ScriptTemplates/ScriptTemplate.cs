namespace DbStudio.Shared.ScriptTemplates;
public class ScriptTemplate {
  public string DisplayName { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public string Filename { get; set; } = string.Empty;
  public DbType DbType { get; set; }
  public bool IsStartup { get; set; }
}
