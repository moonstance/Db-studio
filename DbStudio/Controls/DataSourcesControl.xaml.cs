using DbStudio.Common;
using DbStudio.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DbStudio;
/// <summary>
/// Interaction logic for DataSourcesControl.xaml
/// </summary>
public partial class DataSourcesControl : UserControl, INotifyPropertyChanged {
  public event EventHandler<RavenStore>? DatabaseDoubleClicked;

  private ObservableCollection<DataSource> _dataSources;

  public ObservableCollection<DataSource> DataSources {
    get => _dataSources;
    set { _dataSources = value; OnPropertyChanged(); }
  }

  public event PropertyChangedEventHandler? PropertyChanged;

  protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }

  public ICommand RemoveDataSourceCommand { get; }
  public ICommand EditDataSourceCommand { get; }
  public ICommand NewQueryCommand { get; }
  public ICommand DatabaseDblClickCommand { get; }
  public ICommand RefreshDatabasesCommand { get; }
  public ICommand SetDomainAssemblyForDatabaseCommand { get; }

  public DataSourcesControl() {
    InitializeComponent();

    _dataSources = new ObservableCollection<DataSource>(DatasourcesHelper.LoadFromFile());

    DataContext = this;

    //ExpandAll(trvDataSources);

    RemoveDataSourceCommand = new RelayCommand(parameter => {
      RemoveDataSourceCommandHandler(parameter);
    });

    EditDataSourceCommand = new RelayCommand(parameter => {
      EditDataSourceCommandHandler(parameter);
    });

    NewQueryCommand = new RelayCommand(parameter => NewQueryCommandHandler(parameter) );
    RefreshDatabasesCommand = new RelayCommand(parameter => RefreshDatabasesCommandHandler(parameter));
    DatabaseDblClickCommand = new RelayCommand(parameter => DatabaseDblClickCommandHandler(parameter));
    SetDomainAssemblyForDatabaseCommand = new RelayCommand(parameter => SetDomainAssemblyForDatabaseCommandHandler(parameter));
  }

  private void DatabaseDblClickCommandHandler(object parameter) {
    if (parameter is Database database) {
      InvokeNewQuery(database);
    }
  }

  private void RefreshDatabasesCommandHandler(object parameter) {
    if (parameter is DataSource datasource) {
      RefreshDatabasesForDataSource(datasource);
    }
  }

  private void NewQueryCommandHandler(object parameter) {
    if (parameter is Database database) {
      InvokeNewQuery(database);
    }
  }

  private void InvokeNewQuery(Database database) {
    // Find datasource
    var datasource = _dataSources.FirstOrDefault(x => x.Url == database.ServerUrl);
    if (datasource == null) return;

    DatabaseDoubleClicked?.Invoke(this, new RavenStore(datasource, database));
  }

  private void RemoveDataSourceCommandHandler(object parameter) {
    if (parameter is DataSource datasource) {
      Debug.WriteLine($"Removing datasource: {datasource.Name} - {datasource.Url}");
      DataSources.Remove(datasource);

      DatasourcesHelper.SaveToFile(_dataSources);
    }
    else {
      Debug.WriteLine("RemoveDatasourceCommand received a null or invalid parameter.");
    }
  }

  private void EditDataSourceCommandHandler(object parameter) {
    if (parameter is DataSource datasource) {
      Debug.WriteLine($"Editing datasource: {datasource.Name} - {datasource.Url}");

      var dlg = new DatasourceDialog() {
        DataSource = datasource,
      };

      dlg.Height = 500;

      var result = dlg.ShowDialog();
      if (result ?? false && dlg.IsValid) {
        DatasourcesHelper.SaveToFile(_dataSources);
      }

    }
    else {
      Debug.WriteLine("EditDataSourceCommandHandler received a null or invalid parameter.");
    }
  }

  private void SetDomainAssemblyForDatabaseCommandHandler(object parameter) {
    if (parameter is Database database) {
      // Open dialog to select path to domain assembly
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
        }
        catch (Exception ex) {
          MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        database.DomainAssemblyPath = dlg.FileName;
        DatasourcesHelper.SaveToFile(_dataSources);
      }

    }
  }

  private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
    if (e.NewValue is DataSource dataSource) {
      //MessageBox.Show($"You selected DataSource: {dataSource.Name}");
    }
    else if (e.NewValue is string database) {
      //MessageBox.Show($"You selected Database: {database}");
    }
  }

  private void AddDataSourceButton_Click(object sender, RoutedEventArgs e) {
    // Add datasource
    var dialog = new DatasourceDialog();
    dialog.DataSource = DataSource.CreateDefault();
    var result = dialog.ShowDialog();

    if (result ?? false && dialog.IsValid && dialog.DataSource != null) {
      DataSources.Add(dialog.DataSource);
      RefreshDatabasesForDataSource(dialog.DataSource);
    }
  }

  private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
    // Ensure that the event comes from a TreeViewItem.
    if (sender is TreeViewItem tvi && tvi.DataContext is DataSource dataSource) {
      // For example, call a method to load databases for the dataSource.
      RefreshDatabasesForDataSource(dataSource);
    }

    else if (sender is TreeViewItem tvi2 && tvi2.Header is Database database) {
      InvokeNewQuery(database);
    }
  }

  private void RefreshDatabasesForDataSource(DataSource dataSource) {
    // Clear current databases, if needed
    dataSource.Databases.Clear();

    var databases = RavenHelper.GetDatabases(new RavenStore(dataSource));

    // Update the ObservableCollection on the UI thread
    foreach (var db in databases) {
      dataSource.Databases.Add(new Database() { Name = db, ServerUrl = dataSource.Url} );
    }

    DatasourcesHelper.SaveToFile(_dataSources);
  }

  /// <summary>
  /// Finds the first ancestor of type T in the visual tree.
  /// </summary>
  private static T? FindAncestor<T>(DependencyObject current, bool currentIsSender = false) where T : DependencyObject {
    while (current != null) {
      if (current is T t && !currentIsSender)
        return t;
      current = VisualTreeHelper.GetParent(current);
    }
    return null;
  }

  public void ExpandAll(TreeView treeView) {
    foreach (var item in treeView.Items) {
      if (item is TreeViewItem)
        ExpandAll((TreeViewItem)item);
    }
  }

  public void ExpandAll(TreeViewItem treeViewItem, bool isExpanded = true) {
    var stack = new Stack<TreeViewItem>(treeViewItem.Items.Cast<TreeViewItem>());
    while (stack.Count > 0) {
      TreeViewItem item = stack.Pop();

      foreach (var child in item.Items) {
        var childContainer = child as TreeViewItem;
        if (childContainer == null) {
          childContainer = item.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
        }

        stack.Push(childContainer);
      }

      item.IsExpanded = isExpanded;
    }
  }

  private void CollapseAll(TreeViewItem treeViewItem) {
    ExpandAll(treeViewItem, false);
  }
}
