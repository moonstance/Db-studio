using DbStudio.Shared.ScriptFiles;
using DbStudio.Shared.ScriptTemplates;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace DbStudio {
  /// <summary>
  /// Interaction logic for ScriptFilesControl.xaml
  /// </summary>
  public partial class ScriptFilesControl : UserControl {

    public ObservableCollection<ScriptGroup> ScriptGroups { get; set; }
    public ICommand ScriptFileDblClickCommand { get; }
    public event EventHandler<ScriptFile>? ScriptFileDoubleClicked;

    public ScriptFilesControl() {
      InitializeComponent();

      LoadFiles();

      ScriptFileDblClickCommand = new RelayCommand(parameter => ScriptFileDblClickCommandHandler(parameter));

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

      ScriptGroups = new ObservableCollection<ScriptGroup>(groups);
    }

    private void ScriptFileDblClickCommandHandler(object parameter) {
      if (parameter is ScriptFile scriptfile) {
        InvokeUseScriptFile(scriptfile);
      }
    }

    private void InvokeUseScriptFile(ScriptFile scriptfile) {

      ScriptFileDoubleClicked?.Invoke(this, scriptfile);
    }

  }

  public class ScriptGroup {
    public string DisplayName { get; set; }
    public ObservableCollection<ScriptFile> Scripts { get; set; } = new();
  }

}
