using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class RecordPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      FieldPatterns.SelectMany(pat => pat.Pattern?.NestedPatterns).WhereNotNull();
  }
}