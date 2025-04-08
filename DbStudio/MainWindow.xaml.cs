using System.Collections.ObjectModel;
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

namespace DbStudio;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged {
  private readonly ObservableCollection<DocumentViewModel> _documents;

  private ObservableCollection<QueryEditor> _queryEditors;

  public ObservableCollection<QueryEditor> QueryEditors {
    get => _queryEditors;
    set { _queryEditors = value; OnPropertyChanged(); }
  }


  public event PropertyChangedEventHandler? PropertyChanged;
  public ICommand CloseTabCommand { get; }

  protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }

  public MainWindow() {

    InitializeComponent();

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
