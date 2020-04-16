using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public abstract class FSharpExpressionBase : FSharpCompositeElement, IExpression
  {
    public virtual ConstantValue ConstantValue => ConstantValue.BAD_VALUE;
    public virtual bool IsConstantValue() => false;

    public ExpressionAccessType GetAccessType() => ExpressionAccessType.None;

    public virtual IType Type() => TypeFactory.CreateUnknownType(GetPsiModule());
    public IExpressionType GetExpressionType() => Type();
    public IType GetImplicitlyConvertedTo() => Type();
  }
}
