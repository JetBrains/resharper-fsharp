using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LongIdentSetExpr
  {
    public ITokenNode ReferenceIdentifier => LongIdentifier?.IdentifierToken;
  }
}
