using DbStudio.Shared;
using DbStudio.Shared.ScriptTemplates;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace DbStudio;
/// <summary>
/// Interaction logic for TemplatesControl.xaml
/// </summary>
public partial class TemplatesControl : UserControl {

  public ObservableCollection<TemplateGroup> TemplateGroups { get; set; }
  public event EventHandler<ScriptTemplate>? ScriptTemplateDoubleClicked;

  public ICommand TemplateDblClickCommand { get; }

  public TemplatesControl() {
    InitializeComponent();

    LoadTemplates();

    TemplateDblClickCommand = new RelayCommand(parameter => TemplateDblClickCommandHandler(parameter));

    this.DataContext = this;
  }


  public void LoadTemplates() {
    var templates = TemplateService.LoadTemplates();
    var groups = templates
        .GroupBy(t => t.DbType)
        .Select(g => new TemplateGroup {
          DbType = g.Key,
          Templates = new ObservableCollection<ScriptTemplate>(g)
        });

    TemplateGroups = new ObservableCollection<TemplateGroup>(groups);
  }

  private void TemplateDblClickCommandHandler(object parameter) {
    if (parameter is ScriptTemplate template) {
      InvokeUseTemplate(template);
    }
  }

  private void InvokeUseTemplate(ScriptTemplate template) {

    ScriptTemplateDoubleClicked?.Invoke(this, template);
  }
}

public class TemplateGroup {
  public DbType DbType { get; set; } // Enum
  public string DisplayName => DbType.ToString(); // for label
  public ObservableCollection<ScriptTemplate> Templates { get; set; } = new();
}
