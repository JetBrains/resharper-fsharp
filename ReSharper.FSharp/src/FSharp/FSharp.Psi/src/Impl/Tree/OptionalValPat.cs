using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class OptionalValPat
{
  public override IEnumerable<IFSharpPattern> NestedPatterns =>
    Pattern.NestedPatterns;
}
