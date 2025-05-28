using DbStudio.Common;
using DbStudio.Shared;
using DbStudio.Shared.ScriptFiles;
using DbStudio.Shared.ScriptTemplates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Win32;
using Newtonsoft.Json;
using RoslynPad.Editor;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DbStudio;

/// <summary>
/// Interaction logic for QueryEditor.xaml
/// </summary>
public partial class QueryEditor : UserControl, INotifyPropertyChanged {
  private readonly DocumentViewModel _document;
  private readonly RavenStore _ravenStore;
  private bool _isLoading;

  public event PropertyChangedEventHandler? PropertyChanged;

  public RavenStore RavenStore => _ravenStore;

  public string DocumentTitle { 
    get => _document.Title ?? "new*";
    set {
      if (_document.Title != value) {
        _document.Title = value;
        OnPropertyChanged(nameof(DocumentTitle));
      }
    }
  }


  protected virtual void OnPropertyChanged(string propertyName) {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }

  public QueryEditor(RavenStore ravenStore) {
    InitializeComponent();

    // Create a new DocumentViewModel and add it to the collection.
    _document = new DocumentViewModel(RoslynHelper.GetHost(ravenStore.DomainAssembly != null ? new [] { ravenStore.DomainAssembly } : null));
    DataContext = this;

    CodeEditor.DataContext = _document;
    _ravenStore = ravenStore;

    txtConnectedDatabase.Text = _ravenStore.Database.Name;
    txtConnectedServer.Text = _ravenStore.Url;
    txtDomainAssembly.Text = _ravenStore.DomainAssembly != null ? _ravenStore.DomainAssembly.FullName : "[no domain assembly loaded]";

    CodeEditor.TextArea.DefaultInputHandler.AddBinding(
      new RoutedCommand(),
      ModifierKeys.Control | ModifierKeys.Shift,
      Key.C,
      (sender, e) => CommentSelection()
      );
  }

  private async void OnItemLoaded(object sender, EventArgs e) {
    if (!(sender is RoslynCodeEditor editor && editor.DataContext is DocumentViewModel viewModel)) return;

    editor.Loaded -= OnItemLoaded;
    editor.Focus();

    var workingDirectory = Directory.GetCurrentDirectory();

    var documentText = "";
    if (editor.Text.Length == 0) {
      documentText = GetDocumentTextForNewTab();
    }

    // For dark mode
    //IClassificationHighlightColors classificationColors = new DarkModeHighlightColors();
    //CodeEditor.Background = Brushes.Black;
    // normal ones: new ClassificationHighlightColors()
    IClassificationHighlightColors classificationColors = new ClassificationHighlightColors();

    await editor.InitializeAsync(_document.Host, classificationColors , workingDirectory, documentText, SourceCodeKind.Script);

    

    _isLoading = false;
  }

  private string GetDocumentTextForNewTab() {
    string documentText = @$"using System;
using System.Collections.Generic; 
using System.Linq; 
using DbStudio.Shared;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Linq;


using (var session = RavenHelper.GetSession(SelectedRavenStore))
{{
    //int count = session.Query<Brand>().Count();
    //return count;

    var result = session.Query<Brand>()
    .Where(x => x.Name.StartsWith(""A""))
    .ToListWithStats();

    return result;
}}

";

    // try and find the startup template
    var startTemplate = TemplateService.GetStartupTemplate(DbType.RavenDb); // TODO: create a Database interface IDatabase to use in this class instead of _ravenStore
    if (startTemplate != null) {
      _isLoading = true;
      documentText = TemplateService.ReadTemplate(startTemplate);
      DocumentTitle = "Untitled *";
      _document.IsTemplate = true;
    }

    if (string.IsNullOrEmpty(documentText)) {
      documentText = $@"// Start typing here, or select a template/script from the left panel.";
    }

    return documentText;
  }

  private void ResultDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
    if (ResultDataGrid.SelectedItem is not null) {
      // Serialize the selected item to JSON.
      // You can choose between Newtonsoft.Json or System.Text.Json as needed.
      string json = JsonConvert.SerializeObject(ResultDataGrid.SelectedItem, Formatting.Indented);

      // Open a dialog with the JSON.
      var dialog = new JsonDialog(json);
      //dialog.Owner = this;
      dialog.ShowDialog();
    }
  }

  // This event handler is attached to the "Execute" button click event.
  private async void ExecuteButton_Click(object sender, RoutedEventArgs e) {
    await ExecuteAsync();
  }


  private void SaveDocument_Click(object sender, RoutedEventArgs e) {
    SaveCode();
    
  }

  public void SaveCode() {
    if (string.IsNullOrEmpty(_document.FullPathAndName)) {

      var dlg = new SaveScriptDialog() {
        Heading = "Enter filename"
      };

      dlg.Height = 350;

      var result = dlg.ShowDialog();
      if (result ?? false && !string.IsNullOrEmpty(dlg.Value)) {
        if (dlg.SaveAsTemplate) {
          var savedTemplate = TemplateService.SaveTemplateScript(dlg.Value, CodeEditor.Text, DbType.RavenDb, dlg.UseAsDefaultTemplate);
          if (savedTemplate != null) {
            DocumentTitle = savedTemplate.DisplayName;
          }
        }
        else {
          var script = ScriptFileService.SaveScript(CodeEditor.Text, dlg.Value, DbType.RavenDb);
          DocumentTitle = script.Name;
        }
        
      }
    }
    else {
      SaveCode(_document.FullPathAndName);
      DocumentTitle = DocumentTitle.Replace("*", "");
    }
  }

  public void SetEditorText(string text) {
    CodeEditor.Text = text;
  }

  public void UseScriptFile(ScriptFile scriptfile) {
    CodeEditor.Text = ScriptFileService.ReadScriptFile(scriptfile);
    DocumentTitle = scriptfile.Name;
    _document.FullPathAndName = scriptfile.FullPath;
  }

  public void UseTemplate(ScriptTemplate scriptTemplate) {
    CodeEditor.Text = TemplateService.ReadTemplate(scriptTemplate);
    DocumentTitle = scriptTemplate.DisplayName;
    _document.FullPathAndName = scriptTemplate.Filename;
    _document.IsTemplate = true;
  }

  private void SaveCode(string fullPath) {
    File.WriteAllText(fullPath, CodeEditor.Text);
  }

  private void LoadDocument_Click(object sender, RoutedEventArgs e) {
    LoadDocument();
  }

  public void LoadDocument() {
    var dlg = new OpenFileDialog {
      Filter = "C# script Files (*.csx)|*.csx|C# Files (*.cs)|*.cs|All Files (*.*)|*.*",
      DefaultExt = ".csx"
    };

    if (dlg.ShowDialog() == true) {
      // Read the file and set the CodeEditor's text
      _isLoading = true;
      SetEditorText(File.ReadAllText(dlg.FileName));


      DocumentTitle = dlg.SafeFileName;
      _document.FullPathAndName = dlg.FileName;
      _isLoading = false;
    }
  }

  private void CodeEditor_TextChanged(object sender, EventArgs e) {
    if (!_isLoading)
      DocumentTitle = DocumentTitle.Replace("*", "") + "*";
  }

  public async Task ExecuteAsync() {
    SetRunButtonProps(true);
    ClearUIBeforeExecute();

    await Task.Delay(50);

    await ExecuteInternalAsync();
    SetRunButtonProps(false);
  }

  private async Task ExecuteInternalAsync() {

    StatusTextBlock.Text = "";

    // Retrieve the code from the RoslynPad editor.
    // Depending on the API version, you may need to access a Document or Text property.
    // Here we assume CodeEditor.Text returns the current code.
    string code = CodeEditor.Text;
    if (string.IsNullOrWhiteSpace(code)) {
      ResultTextBox.Text = "No code to execute.";
      return;
    }

    // 1. Identify the assemblies you need:
    var domainAssembly = _ravenStore.DomainAssembly;
    var ravenHelpersAssembly = typeof(RavenHelper).Assembly;
    var ravenAssembly = typeof(Raven.Client.Documents.IDocumentStore).Assembly;

    var referenceAssemblies = new List<Assembly>() {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            ravenHelpersAssembly,
            ravenAssembly
    };

    if (domainAssembly != null) {
      referenceAssemblies.Add(domainAssembly);
    }

    var options = ScriptOptions.Default
        .AddReferences(referenceAssemblies )
        .AddImports(
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "DbStudio.Shared",
            "Raven.Client.Documents",
            "Raven.Client.Documents.Session"
        );


    try {

      // create the globals type for use in RavenHelper
      var globals = new ScriptGlobals() {
        SelectedRavenStore = _ravenStore
      };

      // Create the script from the code entered
      var script = CSharpScript.Create(code, options, typeof(ScriptGlobals));
      // Compile to check for errors before execution
      var diagnostics = script.Compile();
      if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)) {
        string errors = string.Join(Environment.NewLine, diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString()));
        ResultTextBox.Text = "Compilation errors:" + Environment.NewLine + errors;

        spResult.Visibility = Visibility.Visible;

        return;
      }

      // Run the script. If you need to pass in globals, you can define a globals class and pass an instance.
      var scriptState = await script.RunAsync(globals: globals);

      if (scriptState.ReturnValue is IEnumerable<object> resultCollection) {
        ResultDataGrid.ItemsSource = resultCollection;
        StatusTextBlock.Text = "Script executed successfully. Rows: " + resultCollection.Count();
      }
      else if (scriptState.ReturnValue is IQueryResult queryResult) {
        ResultDataGrid.ItemsSource = queryResult.AsEnumerable();
        txtQueryRaw.Text = queryResult.Raw;
        gridStatistics.DataContext = queryResult.QueryStatistics;
        StatusTextBlock.Text = "Script executed successfully. Total results: " + queryResult.QueryStatistics.TotalResults + ". Duration: " + queryResult.QueryStatistics.DurationInMs + "ms";
      }
      else {
        // Handle cases where the result isn't a collection.
        if (scriptState.ReturnValue != null) {
          ResultTextBox.Text = "Result: " + scriptState.ReturnValue.ToString();
        }
        else {
          ResultTextBox.Text = "Script executed successfully, no return value.";
        }
        StatusTextBlock.Text = "";
      }


    }
    catch (CompilationErrorException ex) {
      ResultTextBox.Text = "Compilation error:" + Environment.NewLine +
                           string.Join(Environment.NewLine, ex.Diagnostics);
    }
    catch (Exception ex) {
      ResultTextBox.Text = "Runtime error:" + Environment.NewLine + ex.Message;
    }

    spResult.Visibility = Visibility.Visible;

  }

  private void SetRunButtonProps(bool isRunning) {
    if (isRunning) {
      tbRun.Text = "Running";
      iconRun.Kind = Material.Icons.MaterialIconKind.Pause;
      btnRun.IsEnabled = false;
    }
    else {
      tbRun.Text = "Run";
      iconRun.Kind = Material.Icons.MaterialIconKind.Play;
      btnRun.IsEnabled = true;
    }
  }

  private void ClearUIBeforeExecute() {
    ResultTextBox.Text = string.Empty;
    StatusTextBlock.Text = "Rows: 0";
    gridStatistics.DataContext = null;
    ResultDataGrid.DataContext = null;
  }

  private void CommentSelection() {
    var editor = CodeEditor;
    var document = editor.Document;
    var selectedText = editor.TextArea.Selection;

    if (selectedText.IsEmpty)
      return;

    var startLine = document.GetLineByOffset(selectedText.SurroundingSegment.Offset);
    var endLine = document.GetLineByOffset(selectedText.SurroundingSegment.EndOffset);
    bool isComment = true; // otherwise it's an uncomment.
    string text = document.GetText(startLine);
    if (text.TrimStart().StartsWith("//")) {
      isComment = false;
    }


    using (document.RunUpdate()) {
      for (var line = startLine; line != null && line.Offset <= endLine.Offset; line = line.NextLine) {
        var lineStart = line.Offset;
        if (!isComment) {
          string lineText = document.GetText(line);
          // Find the actual offset of `//` within the line (if there’s leading whitespace)
          int commentOffset = lineText.IndexOf("//", StringComparison.Ordinal);
          
          if (commentOffset > -1) {
            int length = 2;
            if (lineText.Substring(commentOffset+2).StartsWith(" ")) {
              length++;
            }
            document.Remove(line.Offset + commentOffset, length);
          }
        }
        else {
          document.Insert(line.Offset, "// ");
        }

      }
    }
  }

}
