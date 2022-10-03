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
      if (!(fsPattern is IReferencePat refPat))
        return new[] { fsPattern };

      var canBePartial = false;

      while (fsPattern.Parent is IFSharpPattern parent)
      {
        fsPattern = parent;

        if (parent is IOrPat || parent is IAndsPat)
          canBePartial = true;
      }

      if (!canBePartial)
        return new[] { refPat };

      return fsPattern.NestedPatterns.Where(pattern =>
        pattern is IReferencePat nestedRefPat && nestedRefPat.SourceName == refPat.SourceName && pattern.IsDeclaration);
    }

    [CanBeNull]
    public static IBindingLikeDeclaration GetBinding([CanBeNull] this IFSharpPattern pat, bool allowFromParameters, 
      out bool isFromParameter)
    {
      isFromParameter = false;

      if (pat == null)
        return null;

      var node = pat.Parent;
      while (node != null)
      {
        switch (node)
        {
          case IFSharpPattern:
            node = node.Parent;
            break;
          case IPatternParameterDeclarationGroup when allowFromParameters:
            node = node.Parent;
            isFromParameter = true;
            break;
          case IBindingLikeDeclaration binding:
            return binding;
          default:
            return null;
        }
      }

      return null;
    }

    [CanBeNull]
    public static IBindingLikeDeclaration GetBindingFromHeadPattern([CanBeNull] this IFSharpPattern pat) =>
      GetBinding(pat, false, out _);
  }
}
