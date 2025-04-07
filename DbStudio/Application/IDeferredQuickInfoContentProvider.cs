using System.Composition;

namespace RoslynPad.Editor {
  // Define a minimal version of the interface.
  public interface IDeferredQuickInfoContentProvider {
    object GetContent();
  }

  // Export with an explicit contract name matching what the import expects.
  [Export("IDeferredQuickInfoContentProvider", typeof(IDeferredQuickInfoContentProvider))]
  [Shared]
  public class DummyDeferredQuickInfoContentProvider : IDeferredQuickInfoContentProvider {
    public object GetContent() => null;
  }
}
