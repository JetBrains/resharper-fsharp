using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class IdentExpr
  {
    public override ITokenNode IdentifierToken => Identifier as ITokenNode;
  }
}
