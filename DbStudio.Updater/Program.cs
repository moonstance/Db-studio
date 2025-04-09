using System.Diagnostics;
using System.IO.Compression;

namespace DbStudio.Updater;

internal class Program {
  static async Task<int> Main(string[] args) {
    if (args.Length < 2) {
      Console.WriteLine("Missing arguments: <zipPath> <targetAppPath>");
      return 1;
    }

    string zipPath = args[0];
    string targetPath = args[1];

    Console.WriteLine($"Updater started. Zip: {zipPath}, Target: {targetPath}");

    await WaitForDbStudioToExit(targetPath);
    ExtractZipToTempAndReplace(zipPath, targetPath);
    LaunchDbStudio(targetPath);

    return 0;
  }

  private static async Task WaitForDbStudioToExit(string appFolder) {
    const string exeName = "DbStudio.exe";
    int maxWaitMs = 10000;
    int delayMs = 250;

    var exePath = Path.Combine(appFolder, exeName);

    var sw = Stopwatch.StartNew();
    while (sw.ElapsedMilliseconds < maxWaitMs) {
      var isRunning = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exeName))
          .Any(p => {
            try {
              return string.Equals(p.MainModule?.FileName, exePath, StringComparison.OrdinalIgnoreCase);
            }
            catch {
              return false;
            }
          });

      if (!isRunning)
        return;

      await Task.Delay(delayMs);
    }

    Console.WriteLine("DbStudio.exe did not exit in time. Proceeding anyway.");
  }

  private static void ExtractZipToTempAndReplace(string zipPath, string targetPath) {
    string tempExtractPath = Path.Combine(Path.GetTempPath(), "DbStudio_Update");

    if (Directory.Exists(tempExtractPath))
      Directory.Delete(tempExtractPath, true);

    ZipFile.ExtractToDirectory(zipPath, tempExtractPath);

    // Copy all files into app dir (overwrite)
    foreach (var sourceFile in Directory.GetFiles(tempExtractPath, "*", SearchOption.AllDirectories)) {
      string relative = Path.GetRelativePath(tempExtractPath, sourceFile);
      string destFile = Path.Combine(targetPath, relative);
      string destDir = Path.GetDirectoryName(destFile)!;

      Directory.CreateDirectory(destDir);

      // don't try and overwrite myself
      if (sourceFile.EndsWith("updater.exe", StringComparison.InvariantCultureIgnoreCase)) {
        destFile += "_next";
      }

      File.Copy(sourceFile, destFile, overwrite: true);
    }

    // Clean up temp dir if you want
    Directory.Delete(tempExtractPath, true);
  }

  private static void LaunchDbStudio(string appFolder) {
    var exePath = Path.Combine(appFolder, "DbStudio.exe");
    Process.Start(new ProcessStartInfo {
      FileName = exePath,
      UseShellExecute = true,
      WorkingDirectory = appFolder
    });
  }



}
