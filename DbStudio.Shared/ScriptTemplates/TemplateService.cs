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
      return JsonSerializer.Deserialize<List<ScriptTemplate>>(json, SerializerOptions) ?? [];
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

  public static string ReadTemplate(ScriptTemplate template) {
    var dir = Path.GetDirectoryName(TemplatesPath)!;
    var filePath = Path.Combine(dir, template.DbType.ToString(), template.Filename);
    if (File.Exists(filePath)) {
      return File.ReadAllText(filePath);
    }

    return "//Could not find file at " + filePath;
  }
}
