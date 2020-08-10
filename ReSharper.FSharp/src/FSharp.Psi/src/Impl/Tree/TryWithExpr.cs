using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TryWithExpr
  {
    public override IType Type() => GetPsiModule().GetPredefinedType().Void;
  }
}
