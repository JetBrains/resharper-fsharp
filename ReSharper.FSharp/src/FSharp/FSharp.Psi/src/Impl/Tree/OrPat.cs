using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class OrPat
{
  public override IEnumerable<IFSharpPattern> NestedPatterns
  {
    get
    {
      var pattern1Decls = Pattern1?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
      var pattern2Decls = Pattern2?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
      return pattern2Decls.Prepend(pattern1Decls);
    }
  }

  public IList<IFSharpPattern> Patterns
  {
    get
    {
      var result = new List<IFSharpPattern>();

      var pat = this;
      while (pat != null)
      {
        var pat2 = pat.Pattern2;
        if (pat2 != null)
          result.Add(pat2);

        if (pat.Pattern1 is OrPat orPat)
        {
          pat = orPat;
        }
        else
        {
          result.Add(pat.Pattern1);
          pat = null;
        }
      }

      return result;
    }
  }
}
