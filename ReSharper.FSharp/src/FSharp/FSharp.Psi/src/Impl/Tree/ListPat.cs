using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class ListPat
{
  public override IEnumerable<IFSharpPattern> NestedPatterns =>
    Patterns.SelectMany(pat => pat.NestedPatterns);
}
