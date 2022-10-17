using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal static class ReferencePatternUtil
  {
    internal static bool IsDeclaration(this IReferencePat refPat)
    {
      if (refPat.Parent is IBindingLikeDeclaration)
        return true;

      var referenceName = refPat.ReferenceName;
      if (!(referenceName is {Qualifier: null}))
        return false;

      var name = referenceName.ShortName;
      if (!name.IsEmpty() && name[0].IsLowerFast())
        return true;

      var idOffset = refPat.GetNameIdentifierRange().StartOffset.Offset;
      return refPat.FSharpFile.GetSymbolUse(idOffset) == null;
    }
  }
}