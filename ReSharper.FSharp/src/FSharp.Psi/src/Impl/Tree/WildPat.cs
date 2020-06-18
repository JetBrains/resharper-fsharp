using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class WildPat
  {
    public override IType Type() => this.GetFcsType();
  }

  public static class FSharpPatternUtil
  {
    [NotNull]
    public static IType GetFcsType([NotNull] this IFSharpPattern fsPattern) => 
      FSharpTypesUtil.TryGetFcsType(fsPattern);
  }
}
