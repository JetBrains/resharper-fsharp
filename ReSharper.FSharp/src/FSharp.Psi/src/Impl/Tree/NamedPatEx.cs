using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public static class NamedPatEx
  {
    [CanBeNull]
    public static IBinding GetBinding([CanBeNull] this INamedPat pat)
    {
      if (pat == null)
        return null;

      var node = pat.Parent;
      while (node != null)
      {
        switch (node)
        {
          case IBinding binding:
            return binding;
          case ITopParametersOwnerPat parametersOwner when parametersOwner.IsDeclaration:
            return null;
          default:
            node = node.Parent;
            break;
        }
      }

      return null;
    }
  }
}
