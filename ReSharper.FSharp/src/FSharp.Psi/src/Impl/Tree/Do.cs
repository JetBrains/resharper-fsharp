using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class Do : IExpression
  {
    public ConstantValue ConstantValue => ConstantValue.BAD_VALUE;
    public bool IsConstantValue() => false;

    public ExpressionAccessType GetAccessType() => ExpressionAccessType.None;

    public IType Type() => TypeFactory.CreateUnknownType(GetPsiModule());
    public IExpressionType GetExpressionType() => Type();
    public IType GetImplicitlyConvertedTo() => Type();
  }
}
