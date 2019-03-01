using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LongIdentExpr
  {
    public override ITokenNode IdentifierToken => LongIdentifier?.IdentifierToken;
  }
}
