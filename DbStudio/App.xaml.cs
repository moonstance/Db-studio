using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Forms.Design;

namespace DbStudio {
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application {
    public static IServiceProvider Services { get; private set; }

    protected override void OnStartup(StartupEventArgs e) {
      base.OnStartup(e);

      var logPath = Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
          "Moonstance", "DbStudio", "dbstudio.log");

      Log.Logger = new LoggerConfiguration()
          .MinimumLevel.Debug()
          .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
          .CreateLogger();

      Log.Logger.Information("Starting app...");

      var serviceCollection = new ServiceCollection();
      ConfigureServices(serviceCollection);

      Services = serviceCollection.BuildServiceProvider();

      var mainWindow = Services.GetRequiredService<MainWindow>();
      mainWindow.Show();
    }


    private void ConfigureServices(IServiceCollection services) {
      services.AddLogging(loggingBuilder =>
      {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog();
      });

      services.AddSingleton<MainWindow>();
      
      // ... register your viewmodels, data services etc
    }
  }

}
