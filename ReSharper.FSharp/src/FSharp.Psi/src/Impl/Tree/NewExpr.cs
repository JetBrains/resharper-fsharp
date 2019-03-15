using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NewExpr
  {
    public override ITokenNode IdentifierToken =>
      Type.LongIdentifier?.IdentifierToken;
  }
}
