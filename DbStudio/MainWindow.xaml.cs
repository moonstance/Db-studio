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

  //private void AddNewDocument() {
  //  _documents.Add(new DocumentViewModel(RoslynHelper.GetHost()));
  //}

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

  private async void Window_KeyDown(object sender, KeyEventArgs e) {
    if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N) {
      AddNewQueryEditorTab();
      e.Handled = true; // Prevent bubbling
    }
    else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S) {
      // Save
      if (EditorTabControl.SelectedItem is QueryEditor queryEditor) {
        queryEditor.SaveCode();

        // tell ScriptFile control to reload
        scriptFilesControl.LoadFiles();
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
      // Execute doe
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
    UseScriptInSelectedEditor(TemplateService.ReadTemplate(e));
  }

  private void UseScriptInSelectedEditor(string scriptContent) {
    var selectedEditor = EditorTabControl.SelectedItem as QueryEditor;
    if (selectedEditor != null) {
      selectedEditor.SetEditorText(scriptContent);
    }
  }

  //private void scriptFilesControl_ScriptFileDoubleClicked(object sender, Shared.ScriptFiles.ScriptFile e) {
  //  UseScriptInSelectedEditor(ScriptFileService.ReadScriptFile(e));
  //}

  private void scriptFilesControl_ScriptFileDoubleClicked(object sender, OpenScriptFileArg e) {
    OpenScriptFile(e);
  }

  private void OpenScriptFile(OpenScriptFileArg openScriptFileArg) {
    if (!openScriptFileArg.OpenInNewTab) {
      UseScriptInSelectedEditor(ScriptFileService.ReadScriptFile(openScriptFileArg.ScriptFile));
    }
    else {
      var selectedEditor = EditorTabControl.SelectedItem as QueryEditor;
      if (selectedEditor != null) {
        // create a new editor with the same Ravenstore from the current selected one.
        var selectedEditorStore = selectedEditor.RavenStore;
        var newEditor = AddQueryEditor(selectedEditorStore);
        newEditor.SetEditorText(ScriptFileService.ReadScriptFile(openScriptFileArg.ScriptFile));
      }
    }
  }

  private async void btnInstallNewVersion_Click(object sender, RoutedEventArgs e) {

    if (LatestVersionIdOnGithub == null) {
      _logger.LogInformation("Have no clue what latest version on Github is. Aborting install.");
      return;
    }


    try {

      // rename updater files from previous install
      SwapUpdatedFiles();

      // Download the new version
      _logger.LogInformation("Downloading version {version} from github...", LatestVersionIdOnGithub.Name);
      var zipPath = await GitHubService.DownloadRelease(LatestVersionIdOnGithub);

      _logger.LogInformation("Starting updater process. Path to zip is {zipPath}", zipPath);
      var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
      // start new process for Updater.exe
      Process.Start(new ProcessStartInfo {
        FileName = "DbStudio.Updater.exe",
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

    
  }

  private void SwapUpdatedFiles() {
    var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

    var pendingUpdates = Directory.GetFiles(baseDirectory, "*.dll_next")
        .Concat(Directory.GetFiles(baseDirectory, "*.exe_next"));

    foreach (var newFilePath in pendingUpdates) {
      try {
        var originalFilePath = newFilePath.Replace("_next", "");

        if (File.Exists(originalFilePath))
          File.Delete(originalFilePath);

        File.Move(newFilePath, originalFilePath);

        _logger.LogInformation("Replaced file: {Old} with {New}", originalFilePath, newFilePath);
      }
      catch (Exception ex) {
        _logger.LogError(ex, "Failed to replace {File}", newFilePath);
      }
    }
  }

  private async Task CheckForUpdatesAsync() {
    try {

      await Task.Delay(3000);

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
