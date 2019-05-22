using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LongIdentSetExpr
  {
    public override ITokenNode ReferenceIdentifier => LongIdentifier?.IdentifierToken;
  }
}
