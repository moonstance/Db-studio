using DbStudio.Common;
using DbStudio.Shared.ScriptFiles;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DbStudio {
  /// <summary>
  /// Interaction logic for ScriptFilesControl.xaml
  /// </summary>
  public partial class ScriptFilesControl : UserControl {

    public ObservableCollection<ScriptGroup> ScriptGroups { get; set; } = new();
    public ICommand ScriptFileDblClickCommand { get; }
    public ICommand OpenNewScriptCommand { get; }
    public ICommand DeleteScriptCommand { get; }

    public event EventHandler<OpenScriptFileArg>? ScriptFileDoubleClicked;

    public ScriptFilesControl() {
      InitializeComponent();

      LoadFiles();

      ScriptFileDblClickCommand = new RelayCommand(parameter => ScriptFileDblClickCommandHandler(parameter));
      OpenNewScriptCommand = new RelayCommand(parameter => OpenNewScriptCommandHandler(parameter));
      DeleteScriptCommand = new RelayCommand(parameter => DeleteScriptCommandHandler(parameter));

      this.DataContext = this;
    }

    public void LoadFiles() {
      var files = ScriptFileService.GetScripts();
      var groups = files
          .GroupBy(f => f.DbType)
          .OrderBy(g => g.Key.ToString()) // optional sorting
          .Select(g => new ScriptGroup {
            DisplayName = g.Key.ToString(),
            Scripts = new ObservableCollection<ScriptFile>(g.OrderBy(f => f.Name))
          });

      ScriptGroups.Clear(); // = new ObservableCollection<ScriptGroup>(groups);
      foreach (var group in groups) { 
        ScriptGroups.Add(group);
      }
    }

    private void ScriptFileDblClickCommandHandler(object parameter) {
      if (parameter is ScriptFile scriptfile) {
        InvokeUseScriptFile(scriptfile, true);
      }
    }

    private void OpenNewScriptCommandHandler(object parameter) {
      if (parameter is ScriptFile scriptfile) {
        InvokeUseScriptFile(scriptfile, true);
      }
    }

    private void DeleteScriptCommandHandler(object parameter) {
      if (parameter is ScriptFile scriptfile) {
        var result = ScriptFileService.Delete(scriptfile);
        if (!result.Success) {
          MessageBox.Show("Error trying to delete script file. " + result.ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        } else {
          LoadFiles();
        }
      }
    }

    private void InvokeUseScriptFile(ScriptFile scriptfile, bool openInNewTab) {

      ScriptFileDoubleClicked?.Invoke(this, 
        new OpenScriptFileArg() { 
          ScriptFile = scriptfile,
          OpenInNewTab = openInNewTab
        });
    }

  }

  public class ScriptGroup {
    public string DisplayName { get; set; }
    public ObservableCollection<ScriptFile> Scripts { get; set; } = new();
  }

}
