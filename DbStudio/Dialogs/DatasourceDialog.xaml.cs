using DbStudio.Shared;
using DbStudio.Shared.ScriptTemplates;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;

namespace DbStudio {
  /// <summary>
  /// Interaction logic for DatasourceDialog.xaml
  /// </summary>
  public partial class DatasourceDialog : Window, INotifyPropertyChanged {

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ScriptTemplate> ScriptTemplates { get; set; }
    public IEnumerable<DbType> DbTypeValues => Enum.GetValues(typeof(DbType)).Cast<DbType>();

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public DatasourceDialog() {
      InitializeComponent();
      DataSource = new DataSource();

      PopulateScriptTemplates();

      this.DataContext = this;
    }

    private void PopulateScriptTemplates() {
      var templates = TemplateService.LoadTemplates().ToList();
      templates.Insert(0, new ScriptTemplate {
        DisplayName = "(None)",
        Description = "No template",
        Filename = "",
        DbType = DbType.Other
      });
      ScriptTemplates = new ObservableCollection<ScriptTemplate>(templates);

      DataSource.ScriptTemplate = templates[0];
    }

    private DataSource _datasource;
    public DataSource DataSource {
      get => _datasource;
      set { _datasource = value; OnPropertyChanged(); }
    }

    public bool IsValid {
      get {
        var d = DataSource;
        if (!string.IsNullOrEmpty(Name)
          && Uri.TryCreate(DataSource.Url, UriKind.Absolute, out var uriResult) && uriResult != null) {

          return true;
        }
        return false;
      }
    }

    public void OkButton_Click(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) {
      DialogResult = false;
    }

    private void OpenFileDlgButton_Click(object sender, RoutedEventArgs e) {
      // Create OpenFileDialog 
      Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

      // Set filter for file extension and default file extension 
      dlg.DefaultExt = ".pfx";
      dlg.Filter = "pfex Files (*.pfx)|*.pfx";

      // Display OpenFileDialog by calling ShowDialog method 
      Nullable<bool> result = dlg.ShowDialog();

      // Get the selected file name and display in a TextBox 
      if (result == true) {
        // Open document 
        DataSource.CertificatePath = dlg.FileName;
        txtCertPath.Text = dlg.FileName;
      }
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
      DataSource.CertificatePassword = PasswordProtector.EncryptPassword(txtPassword.Password);
    }

    private void btnFindDomainAssembly_Click(object sender, RoutedEventArgs e) {
      // Create OpenFileDialog 
      Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

      // Set filter for file extension and default file extension 
      dlg.DefaultExt = ".dll";
      dlg.Filter = "Assembly files (*.dll)|*.dll";

      // Display OpenFileDialog by calling ShowDialog method 
      Nullable<bool> result = dlg.ShowDialog();

      // Get the selected file name and display in a TextBox 
      if (result == true) {
        
        try {
          var asm = Assembly.LoadFrom(dlg.FileName);
          lblAssemblyInfo.Text = $"Name: {asm.GetName()} Version: {asm.GetName().Version}";
        } catch (Exception ex) {
          lblAssemblyInfo.Text = "Unable to load assembly. " + ex.Message;
        }

        DataSource.DomainAssemblyPath = dlg.FileName;
        txtDomainAssemblyPath.Text = dlg.FileName;
      }
    }
  }
}
