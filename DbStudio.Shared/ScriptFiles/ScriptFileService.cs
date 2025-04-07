using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using static Raven.Client.Constants;

namespace DbStudio.Shared.ScriptFiles;
public class ScriptFileService {
  private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Moonstance", "DbStudio");
  private static readonly string ScriptFileFolder = Path.Combine(AppDataFolder, "ScriptFiles");



  static ScriptFileService() {
    // Ensure the AppData folder exists
    if (!Directory.Exists(ScriptFileFolder)) {
      Directory.CreateDirectory(ScriptFileFolder);
    }
  }

  public static ScriptFile SaveScript(string scriptFileContent, string name, DbType dbType) {
    // ensure folder exist
    string folder = Path.Combine(ScriptFileFolder, dbType.ToString());
    if (!Directory.Exists(folder)) {
      Directory.CreateDirectory(folder);
    }

    // trim and clean name
    name = SanitizeFileName(name);

    // ensure name ends with .csx extension
    name = EnsureCorrectExtension(name);

    try {
      var fullPath = Path.Combine(folder, name);
      File.WriteAllText(fullPath, scriptFileContent);

      return new ScriptFile() {
        Name = name,
        DbType = dbType,
        FullPath = fullPath,
        LastModified = DateTime.Now,
      };
    }
    catch (Exception ex) {
      // implement some logging later
      Debug.WriteLine($"Failed to save script: {ex.Message}");
    }

    return null;
  }

  public static string SanitizeFileName(string input) {
    var invalidChars = Path.GetInvalidFileNameChars();
    var cleaned = new string(input.Where(c => !invalidChars.Contains(c)).ToArray());
    return cleaned.Trim(); // optional: also remove whitespace from start/end
  }

  private static string EnsureCorrectExtension(string name) {
    const string fileExtension = ".csx";
    string extension = Path.GetExtension(name);

    if (string.IsNullOrWhiteSpace(extension)) {
      return name + fileExtension;
    }
    else if (!extension.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)) {
      return Path.GetFileNameWithoutExtension(name) + fileExtension;
    }

    return name;
  }

  public static string ReadScriptFile(ScriptFile scriptFile) {
    if (File.Exists(scriptFile.FullPath)) {
      return File.ReadAllText(scriptFile.FullPath);
    }

    return string.Empty;
  }

  public static IEnumerable<ScriptFile> GetScripts() {
    List<ScriptFile> scripts = new List<ScriptFile>();

    var folders = Directory.GetDirectories(ScriptFileFolder);
    foreach (var folder in folders) {
      // each folder should be mappable to a DbType
      var folderName = Path.GetFileName(folder);
      if (Enum.TryParse<DbType>(folderName, true, out var dbType)) {
        // list all files (*.csx)
        var f = new System.IO.DirectoryInfo(folder);
        foreach (var file in f.GetFiles("*.csx")) {
          scripts.Add(new ScriptFile() {
            DbType = dbType,
            FullPath = file.FullName,
            LastModified = file.LastWriteTime,
            Name = file.Name,
          });
        }
      }
    }

    return scripts;
  }

  public static DeleteResult Delete(ScriptFile scriptfile) {
    try {
      File.Delete(scriptfile.FullPath);
      return new DeleteResult() { Success = true };
    } catch (Exception ex) {
      return new DeleteResult() { Success = false, ErrorMessage = ex.Message };
    }
  }
}

public class DeleteResult {
  public bool Success { get; set; }
  public string? ErrorMessage { get; set; }
}
