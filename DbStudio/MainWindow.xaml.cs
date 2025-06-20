﻿using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using RoslynPad.Editor;
using DbStudio.Shared;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using DbStudio.Shared.ScriptTemplates;
using DbStudio.Shared.ScriptFiles;
using DbStudio.Common;
using System.Reflection;
using DbStudio.Shared.Github;
using DbStudio.Shared.Github.Models;
using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.Compression;
using Sparrow.Json;

namespace DbStudio;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged {
  public Version CurrentVersion => Assembly.GetEntryAssembly()!.GetName().Version!;
  public ICommand CloseTabCommand { get; }
  public event PropertyChangedEventHandler? PropertyChanged;


  
  private bool _updateAvailable = false;
  public bool UpdateAvailable {
    get => _updateAvailable;
    set { _updateAvailable = value; OnPropertyChanged(); }
  }

  private ObservableCollection<QueryEditor> _queryEditors;
  public ObservableCollection<QueryEditor> QueryEditors {
    get => _queryEditors;
    set { _queryEditors = value; OnPropertyChanged(); }
  }



  private readonly ObservableCollection<DocumentViewModel> _documents;
  private readonly ILogger<MainWindow> _logger;
  private Release? LatestVersionIdOnGithub = null;

  protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }

  public MainWindow(ILogger<MainWindow> logger) {

    InitializeComponent();
    _logger = logger;

    _documents = new ObservableCollection<DocumentViewModel>();
    _queryEditors = new ObservableCollection<QueryEditor>();



    // Set the DataContext for the window so that {Binding QueryEditors} works.
    DataContext = this;


    ScrollViewerFix.AttachMouseWheelSupport(ctrlDatasources, MyScrollViewer);
    ScrollViewerFix.AttachMouseWheelSupport(ctrlTemplates, MyScrollViewer);
    ScrollViewerFix.AttachMouseWheelSupport(scriptFilesControl, MyScrollViewer);


    // Define CloseTabCommand
    CloseTabCommand = new RelayCommand(tab => {
      if (tab is TabItem tabItem && tabItem.DataContext is QueryEditor queryEditor) {
        Debug.WriteLine($"Closing tab: {queryEditor.DocumentTitle}");
        QueryEditors.Remove(queryEditor);
      }
      else {
        Debug.WriteLine("CloseTabCommand received a null or invalid parameter.");
      }
    });

    Loaded += (s, e) => {
      Task.Run( () => CheckForUpdatesAsync() );
    };
    
  }


  private void MainWindow_Loaded(object sender, RoutedEventArgs e) {

    EditorTabControl.SelectedIndex = 0;

  }

  private RavenStore? GetSelectedDbStore() {
    var selectedTab = EditorTabControl.SelectedItem as QueryEditor;
    if (selectedTab != null) {
      return selectedTab.RavenStore;
    }
    return null;
  }

  private void AddNewQueryEditorTab() {
    var ravenStore = GetSelectedDbStore();

    if (ravenStore != null) {
      AddQueryEditor(ravenStore);
    }
    else {
      MessageBox.Show("No ravenstore was selected");
    }
  }

  private QueryEditor AddQueryEditor(RavenStore ravenStore) {
    Mouse.OverrideCursor = Cursors.Wait;

    var queryEditor = new QueryEditor(ravenStore);
    _queryEditors.Add(queryEditor);

    EditorTabControl.SelectedIndex = _queryEditors.Count - 1;

    Mouse.OverrideCursor = null;

    return queryEditor;
  }
  private void OpenScriptFile(OpenScriptFileArg openScriptFileArg) {
    if (!openScriptFileArg.OpenInNewTab) {
      UseScriptInSelectedEditor(openScriptFileArg.ScriptFile);
    }
    else {
      var selectedEditor = EditorTabControl.SelectedItem as QueryEditor;
      if (selectedEditor != null) {
        // create a new editor with the same Ravenstore from the current selected one.
        var selectedEditorStore = selectedEditor.RavenStore;
        var newEditor = AddQueryEditor(selectedEditorStore);
        newEditor.UseScriptFile(openScriptFileArg.ScriptFile);
      }
    }
  }
  private void UseScriptInSelectedEditor(ScriptFile scriptFile) {
    var selectedEditor = EditorTabControl.SelectedItem as QueryEditor;
    if (selectedEditor != null) {
      selectedEditor.UseScriptFile(scriptFile);
    }
  }
  private void UseScriptInSelectedEditor(ScriptTemplate scriptTemplate) {
    var selectedEditor = EditorTabControl.SelectedItem as QueryEditor;
    if (selectedEditor != null) {
      selectedEditor.UseTemplate(scriptTemplate);
    }
  }

  private void UseScriptInNewEditor(ScriptTemplate scriptTemplate) {

    var selectedEditor = EditorTabControl.SelectedItem as QueryEditor;
    if (selectedEditor != null) {
      // create a new editor with the same Ravenstore from the current selected one.
      var selectedEditorStore = selectedEditor.RavenStore;
      var newEditor = AddQueryEditor(selectedEditorStore);
      newEditor.UseTemplate(scriptTemplate);
    }
  }

  #region Event handlers

  private async void Window_KeyDown(object sender, KeyEventArgs e) {
    if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N) {
      AddNewQueryEditorTab();
      e.Handled = true; // Prevent bubbling
    }
    else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S) {
      // Save
      if (EditorTabControl.SelectedItem is QueryEditor queryEditor) {
        queryEditor.SaveCode();

        // tell ScriptFile and Templates control to reload
        scriptFilesControl.LoadFiles();
        ctrlTemplates.LoadTemplates();
        e.Handled = true; // Prevent bubbling
      }
    }
    else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O) {
      // Open
      if (EditorTabControl.SelectedItem is QueryEditor queryEditor) {
        queryEditor.LoadDocument();
        e.Handled = true; // Prevent bubbling
      }
    }
    else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Return) {
      // Execute script
      if (EditorTabControl.SelectedItem is QueryEditor queryEditor) {
        try {
          await queryEditor.ExecuteAsync();
          e.Handled = true; // Prevent bubbling
        }
        catch (Exception ex) {
          MessageBox.Show(ex.ToString());
        }

      }
    }

  }

  private void DataSourcesControl_DatabaseDoubleClicked(object sender, RavenStore e) {
    AddQueryEditor(e);
  }

  private void TemplatesControl_ScriptTemplateDoubleClicked(object sender, Shared.ScriptTemplates.ScriptTemplate e) {
    UseScriptInNewEditor(e);
  }

  private void scriptFilesControl_ScriptFileDoubleClicked(object sender, OpenScriptFileArg e) {
    OpenScriptFile(e);
  }

  #endregion


  #region Update

  private async void btnInstallNewVersion_Click(object sender, RoutedEventArgs e) {

    if (LatestVersionIdOnGithub == null) {
      _logger.LogInformation("Have no clue what latest version on Github is. Aborting install.");
      return;
    }


    try {
      ctrlUpdateSpinner.Visibility = Visibility.Visible;

      var tempExtractZipPath = await DownloadAndUnzipUpdateAsync();
      if (string.IsNullOrWhiteSpace(tempExtractZipPath)) {
        ctrlUpdateSpinner.Visibility = Visibility.Collapsed;
        return;
      }

      var zipPath = Path.Combine(tempExtractZipPath, "DbStudio.zip");
      var updaterPath = Path.Combine(tempExtractZipPath, "DbStudio.Updater.exe");

      _logger.LogInformation("Starting updater process. Path to zip is {zipPath}", zipPath);
      var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
      // start new process for Updater.exe
      Process.Start(new ProcessStartInfo {
        FileName = updaterPath,
        Arguments = $"\"{zipPath}\" \"{baseDirectory}\"",
        UseShellExecute = false,
      });

      _logger.LogInformation("Shutting down DbStudio before install.");
      // kill this application
      Application.Current.Shutdown();

    }
    catch (Exception ex) {
      // TODO: Log it
      _logger.LogError(ex, "Error installing new version.");
    }

    ctrlUpdateSpinner.Visibility = Visibility.Collapsed;
  }

  private async Task<string?> DownloadAndUnzipUpdateAsync() {
    if (LatestVersionIdOnGithub == null) {
      _logger.LogInformation("Have no clue what latest version on Github is. Aborting install.");
      return null;
    }

    _logger.LogInformation("Downloading version {version} from github...", LatestVersionIdOnGithub.Name);
    var zipPath = await GitHubService.DownloadRelease(LatestVersionIdOnGithub);

    if (zipPath == null) {
      _logger.LogInformation("Could not download update.. aborting");
      return null;
    }

    string tempExtractPath = Path.GetDirectoryName(zipPath)!;

    // Extract update
    ZipFile.ExtractToDirectory(zipPath, tempExtractPath);

    string updaterZipPath = Path.Combine(tempExtractPath, "Updater.zip");
    ZipFile.ExtractToDirectory(updaterZipPath, tempExtractPath, true);

    return tempExtractPath;

  }

  private async Task CheckForUpdatesAsync() {
    try {

      await Task.Delay(1000);

      // This runs off the UI thread
      LatestVersionIdOnGithub = await GitHubService.GetLatestVersionAsync();

      if (LatestVersionIdOnGithub == null) {
        return;
      }

      // parse
      var latestVersion = new Version(LatestVersionIdOnGithub.Name.Replace("v", ""));

      if (latestVersion > CurrentVersion) {
        Dispatcher.Invoke(() =>
        {
          UpdateAvailable = true;
        });
      }
    }
    catch (Exception ex) {
      Debug.WriteLine("Update check failed: " + ex.Message);
    }
  }

  #endregion
}

public static class ScrollViewerFix {
  public static void AttachMouseWheelSupport(FrameworkElement child, ScrollViewer scrollViewer) {
    child.PreviewMouseWheel += (sender, e) =>
    {
      if (!e.Handled) {
        // Raise event manually on ScrollViewer
        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) {
          RoutedEvent = UIElement.MouseWheelEvent,
          Source = sender
        };
        scrollViewer.RaiseEvent(eventArg);
      }
    };
  }
}
