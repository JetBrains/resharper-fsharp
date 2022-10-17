using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ListConsPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns
    {
      get
      {
        var pattern1Decls = HeadPattern?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
        var pattern2Decls = TailPattern?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
        return pattern2Decls.Prepend(pattern1Decls);
      }
    }
  }
}