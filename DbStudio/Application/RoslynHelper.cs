using DbStudio.Shared;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynPad.Roslyn;
using System.Collections.Immutable;
using System.Reflection;

namespace DbStudio {
  public static class RoslynHelper {
    private static RoslynHost _host;

    public static RoslynHost GetHost(IEnumerable<Assembly>? customAssemblies = null) {
      if (_host != null)
        return _host;

      var hostReferences = new List<Assembly> {
         typeof(object).Assembly,
        typeof(System.Text.RegularExpressions.Regex).Assembly,
        typeof(System.Linq.Expressions.BinaryExpression).Assembly,
        typeof(System.Collections.Generic.KeyValuePair).Assembly,
        typeof(Enumerable).Assembly,
        typeof(RavenHelper).Assembly,
        typeof(Raven.Client.Documents.IDocumentStore).Assembly,
         Assembly.Load("System.Runtime"),
         Assembly.Load("System.Collections")
      };

      if (customAssemblies != null) {
        hostReferences.AddRange(customAssemblies);
      }

      _host = new CustomRoslynHost(additionalAssemblies:
       [
           Assembly.Load("RoslynPad.Roslyn.Windows"),
          Assembly.Load("RoslynPad.Editor.Windows"),
       ],
       RoslynHostReferences.NamespaceDefault.With(assemblyReferences: hostReferences )
     );

      return _host;
    }





    private class CustomRoslynHost : RoslynHost {
      private bool _addedAnalyzers;

      public CustomRoslynHost(IEnumerable<Assembly>? additionalAssemblies = null, RoslynHostReferences? references = null, ImmutableHashSet<string>? disabledDiagnostics = null) : base(additionalAssemblies, references, disabledDiagnostics) {
      }

      protected override IEnumerable<AnalyzerReference> GetSolutionAnalyzerReferences() {
        if (!_addedAnalyzers) {
          _addedAnalyzers = true;
          return base.GetSolutionAnalyzerReferences();
        }

        return Enumerable.Empty<AnalyzerReference>();
      }
    }
  }
}
