using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public abstract class SetExpressionBase : DummyExpression
  {
    public virtual ITokenNode ReferenceIdentifier => null;
  }
}
