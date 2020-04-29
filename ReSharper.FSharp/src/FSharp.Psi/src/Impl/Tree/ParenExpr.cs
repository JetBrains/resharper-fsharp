using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ParenExpr
  {
    public override IType Type() =>
      InnerExpression?.Type() ??
      TypeFactory.CreateUnknownType(GetPsiModule());

    public override bool IsConstantValue() =>
      InnerExpression?.IsConstantValue() ?? false;
  }
}
