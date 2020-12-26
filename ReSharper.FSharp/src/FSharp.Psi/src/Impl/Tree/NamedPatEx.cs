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
          case IFSharpPattern:
            node = node.Parent;
            break;
          case IBinding binding:
            return binding;
          default:
            return null;
        }
      }

      return null;
    }
  }
}
