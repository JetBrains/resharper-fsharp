using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TryFinallyExpr
  {
    public override IType Type() => GetPsiModule().GetPredefinedType().Void;
  }
}
