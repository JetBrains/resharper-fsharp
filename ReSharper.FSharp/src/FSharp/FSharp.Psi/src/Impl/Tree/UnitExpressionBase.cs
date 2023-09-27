using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public abstract class UnitExpressionBase : FSharpExpressionBase
  {
    public override IType Type() =>
      GetPsiModule().GetPredefinedType().Void;
  }
}
