using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class IfThenElseExpr
  {
    public override IType Type() =>
      ElseExpr != null
        ? ThenExpr?.Type() ?? base.Type()
        : GetPsiModule().GetPredefinedType().Void;
  }
}
