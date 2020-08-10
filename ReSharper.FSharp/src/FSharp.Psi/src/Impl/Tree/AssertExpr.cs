using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class AssertExpr
  {
    public override IType Type() => GetPsiModule().GetPredefinedType().Void;
  }
}
