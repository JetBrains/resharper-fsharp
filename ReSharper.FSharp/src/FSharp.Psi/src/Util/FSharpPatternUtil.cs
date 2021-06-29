using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class FSharpPatternUtil
  {
    [ItemNotNull]
    public static IEnumerable<IFSharpPattern> GetPartialDeclarations([NotNull] this IFSharpPattern fsPattern)
    {
      if (!(fsPattern is INamedPat namedPattern))
        return new[] { fsPattern };

      var canBePartial = false;

      while (fsPattern.Parent is IFSharpPattern parent)
      {
        fsPattern = parent;

        if (parent is IOrPat || parent is IAndsPat)
          canBePartial = true;
      }

      if (!canBePartial)
        return new[] { namedPattern };

      return fsPattern.NestedPatterns.Where(pattern =>
        pattern is INamedPat namedPat && namedPat.SourceName == namedPattern.SourceName && pattern.IsDeclaration);
    }

    [CanBeNull]
    public static IBindingLikeDeclaration GetBinding([CanBeNull] this IFSharpPattern pat)
    {
      if (pat == null)
        return null;

      var node = pat.Parent;
      while (node != null)
      {
        switch (node)
        {
          case IFSharpPattern _:
            node = node.Parent;
            break;
          case IBindingLikeDeclaration binding:
            return binding;
          default:
            return null;
        }
      }

      return null;
    }
  }
}
