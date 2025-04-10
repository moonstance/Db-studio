using Serilog;
using System.Diagnostics;
using System.IO.Compression;

namespace DbStudio.Updater;

internal class Program {

  private static ILogger _logger;
  private static string _logPath;

  static async Task<int> Main(string[] args) {
    try {
      _logPath = Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
          "Moonstance", "DbStudio", "dbstudio-updater.log");

      Log.Logger = new LoggerConfiguration()
          .MinimumLevel.Debug()
          .WriteTo.File(_logPath, rollingInterval: RollingInterval.Day)
          .CreateLogger();

      _logger = Log.Logger;

      _logger.Information("Starting updater. Args {args}", args);


      if (args.Length < 2) {
        Console.WriteLine("Missing arguments: <zipPath> <targetAppPath>");
        _logger.Error("Missing arguments: <zipPath> <targetAppPath>");
        return 1;
      }


      string zipPath = args[0];
      string targetPath = args[1].Trim();
      targetPath = targetPath.Replace("\"", "");
      if (targetPath.EndsWith("\\")) {
        targetPath = targetPath.Substring(0, targetPath.Length - 1);
      }

      Console.WriteLine($"Updater started. Zip: {zipPath}, Target: {targetPath}");

      var waitResult = await WaitForDbStudioToExit(targetPath);
      _logger.Information("Waiting result: {waiting}", waitResult);

      ExtractZipToTempAndReplace(zipPath, targetPath);
      LaunchDbStudio(targetPath);

    }
    catch (Exception ex) {
      _logger.Error(ex, "Error in install.");
      Log.CloseAndFlush();
    }
    await Task.Delay(1500);


    return 0;
  }

  private static async Task<bool> WaitForDbStudioToExit(string appFolder) {
    try {
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
          return true;

        await Task.Delay(delayMs);
      }

      Console.WriteLine("DbStudio.exe did not exit in time. Proceeding anyway.");
      _logger.Error("DbStudio.exe did not exit in time.Proceeding anyway.");

      return true;
    }
    catch (Exception ex) {
      _logger.Error(ex, "Error when waiting for DbStudio process to exit");
    }
    return false;

  }

  private static void ExtractZipToTempAndReplace(string zipPath, string targetPath) {
    try {
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
    catch (Exception ex) {
      Console.WriteLine("Error in extracting and copying the new installation files. More info in logfile at {logPath}", _logPath);
      _logger.Error(ex, "Error in extracting and copying the new installation files");
    }
  }

  private static void LaunchDbStudio(string appFolder) {
    try {
      var exePath = Path.Combine(appFolder, "DbStudio.exe");
      Process.Start(new ProcessStartInfo {
        FileName = exePath,
        UseShellExecute = true,
        WorkingDirectory = appFolder
      });
    }
    catch (Exception ex) {
      Console.WriteLine("Error launching DbStudio after install. More info in logfile at {logPath}", _logPath);
      _logger.Error(ex, "Error launching DbStudio after install");
    }
  }



}
