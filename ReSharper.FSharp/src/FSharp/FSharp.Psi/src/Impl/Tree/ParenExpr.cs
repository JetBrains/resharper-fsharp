using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ParenExpr
  {
    public override IType Type() =>
      InnerExpression?.Type() ?? base.Type();

    public override ConstantValue ConstantValue =>
      InnerExpression?.ConstantValue ?? base.ConstantValue;

    public override bool IsConstantValue() =>
      InnerExpression?.IsConstantValue() ?? false;
  }
}
