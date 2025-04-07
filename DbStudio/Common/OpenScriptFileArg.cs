using DbStudio.Shared.ScriptFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbStudio.Common;
public class OpenScriptFileArg {
  public ScriptFile ScriptFile { get; set; }
  public bool OpenInNewTab { get; set; }
}
