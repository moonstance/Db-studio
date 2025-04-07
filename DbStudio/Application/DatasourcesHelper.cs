using System.IO;
using DbStudio.Shared;
using Newtonsoft.Json;

namespace DbStudio;

public static class DatasourcesHelper {
  private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Moonstance", "DbStudio");
  private static readonly string DatasourcesFilePath = Path.Combine(AppDataFolder, "datasources.json");

  static DatasourcesHelper() {
    // Ensure the AppData folder exists
    if (!Directory.Exists(AppDataFolder)) {
      Directory.CreateDirectory(AppDataFolder);
    }
  }

  public static IList<DataSource> LoadFromFile() {
    if (!File.Exists(DatasourcesFilePath)) {
      return new List<DataSource>();
    }

    var json = File.ReadAllText(DatasourcesFilePath);
    var list = JsonConvert.DeserializeObject<List<DataSource>>(json) ?? new List<DataSource>();

    return list.Where(x => x != null).ToList();
  }

  public static void SaveToFile(IList<DataSource> dataSources) {
    var json = JsonConvert.SerializeObject(dataSources, Formatting.Indented);
    File.WriteAllText(DatasourcesFilePath, json);
  }
}
