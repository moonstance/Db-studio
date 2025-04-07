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
  public partial class InputStringDialog : Window {

    public string Value { get; set; }
    public string Heading { get; set; } = "Enter string";


    public InputStringDialog() {
      InitializeComponent();

      

      this.DataContext = this;
    }

    public void OkButton_Click(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) {
      DialogResult = false;
    }
  }
}
