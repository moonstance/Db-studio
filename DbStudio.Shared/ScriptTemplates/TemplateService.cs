using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DbStudio.Shared.ScriptTemplates;
public class TemplateService {

  private static readonly JsonSerializerOptions SerializerOptions = new() {
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
  };

  private static string TemplatesPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                                                     "Templates",
                                                     "templates.json");

  public static IEnumerable<ScriptTemplate> LoadTemplates() {
    if (!File.Exists(TemplatesPath))
      return [];

    try {
      var json = File.ReadAllText(TemplatesPath);
      var templates = JsonSerializer.Deserialize<List<ScriptTemplate>>(json, SerializerOptions) ?? [];

      return templates;
    }
    catch (Exception ex) {
      // TODO: optionally log or show message to user
      return [];
    }
  }

  public static void SaveTemplates(IEnumerable<ScriptTemplate> templates) {
    var json = JsonSerializer.Serialize(templates, SerializerOptions);

    var dir = Path.GetDirectoryName(TemplatesPath)!;
    if (!Directory.Exists(dir))
      Directory.CreateDirectory(dir);

    File.WriteAllText(TemplatesPath, json);
  }

  public static ScriptTemplate? SaveTemplateScript(string name, string script, DbType dbType, bool isStartup) {

    var templatesFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                                                     "Templates");

    try {
      var template = new ScriptTemplate() {
        DbType = dbType,
        DisplayName = name,
        IsStartup = isStartup,
        Filename = Path.Combine(templatesFolder, dbType.ToString(), name)
      };

      File.WriteAllText(template.Filename, script);

      // add template to templates
      var allTemplates = LoadTemplates().ToList();
      allTemplates.Add(template);

      SaveTemplates(allTemplates);
      return template;
    }
    catch { }

    return null;
  }

  public static string ReadTemplate(ScriptTemplate template) {
    var filePath = GetTemplateFullPath(template);

    if (File.Exists(filePath)) {
      return File.ReadAllText(filePath);
    }

    return "//Could not find file at " + filePath;
  }

  public static ScriptTemplate? GetStartupTemplate(DbType dbType) {
    var templates = LoadTemplates();
    return templates.SingleOrDefault(x => x.DbType == dbType && x.IsStartup);
  }

  public static string GetTemplateFullPath(ScriptTemplate template) {
    var dir = Path.GetDirectoryName(TemplatesPath)!;
    return Path.Combine(dir, template.DbType.ToString(), template.Filename);
  }
}
