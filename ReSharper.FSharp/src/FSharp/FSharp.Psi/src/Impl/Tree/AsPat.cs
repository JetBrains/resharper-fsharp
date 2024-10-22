using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class AsPat
{
  public override IEnumerable<IFSharpPattern> NestedPatterns
  {
    get
    {
      var pattern1Decls = LeftPattern?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
      var pattern2Decls = RightPattern?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
      return pattern2Decls.Prepend(pattern1Decls);
    }
  }
}
