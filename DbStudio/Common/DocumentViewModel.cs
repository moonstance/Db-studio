using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using RoslynPad.Editor;
using RoslynPad.Roslyn;

namespace DbStudio.Common {
  internal class DocumentViewModel : INotifyPropertyChanged {
    private string _title;
    private string _text;
    private DocumentId _documentId;
    private RoslynCodeEditor _editor;

    public DocumentViewModel(RoslynHost host) {
      Host = host;
      Title = "New *";
      Text = string.Empty;
    }

    /// <summary>
    /// Gets the RoslynHost associated with this document.
    /// </summary>
    public RoslynHost Host { get; }

    /// <summary>
    /// The title for the tab header.
    /// </summary>
    public string Title {
      get => _title;
      set { _title = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The current text (code) in this document.
    /// </summary>
    public string Text {
      get => _text;
      set { _text = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The DocumentId assigned by the RoslynHost once the document is initialized.
    /// </summary>
    public DocumentId DocumentId {
      get => _documentId;
      set { _documentId = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// A reference to the RoslynCodeEditor instance hosting this document's UI.
    /// </summary>
    public RoslynCodeEditor Editor {
      get => _editor;
      set { _editor = value; OnPropertyChanged(); }
    }

    public string FullPathAndName { get; internal set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
