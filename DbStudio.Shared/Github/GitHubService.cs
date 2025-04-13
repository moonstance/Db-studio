using DbStudio.Shared.Github.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DbStudio.Shared.Github;
public static class GitHubService {

  private static readonly JsonSerializerOptions SerializerOptions = new() {
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
  };

  public static async Task<Release?> GetLatestVersionAsync() {

    using var client = new HttpClient();
    try {
      client.DefaultRequestHeaders.UserAgent.ParseAdd("DbStudio-Updater");
      client.BaseAddress = new Uri("https://api.github.com");

      var json = await client.GetStringAsync("/repos/moonstance/db-studio/releases/latest");
      if (string.IsNullOrEmpty(json)) {
        // Log this
        return null;
      }

      return JsonSerializer.Deserialize<Release>(json, SerializerOptions);
    }
    catch (Exception ex) {
      // TODO: Log when we have logger in place
      return null;
    }
  }

  /// <summary>
  /// Download the reelase from github and returns the local path
  /// </summary>
  /// <param name="release"></param>
  /// <returns>The path of the downloaded release zip</returns>
  public static async Task<string?> DownloadRelease(Release release) {
    var asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
    if (asset == null)
      return null;

    using var client = new HttpClient();
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DbStudio-Updater");

    var downloadUrl = asset.Browser_Download_Url;

    // Where to save it
    string tempUpdatePath = Path.Combine(Path.GetTempPath(), "DbStudio_Update");
    if (Directory.Exists(tempUpdatePath))
      Directory.Delete(tempUpdatePath, true);

    Directory.CreateDirectory(tempUpdatePath);

    string targetPath = Path.Combine(tempUpdatePath, asset.Name);

    var response = await client.GetAsync(downloadUrl);
    if (!response.IsSuccessStatusCode) {
      // TODO: Log it
      return null;
    }
    
    await using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
    await response.Content.CopyToAsync(fs);

    return targetPath;
  }
}
