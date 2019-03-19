using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class RecordExprBinding : ISetExpr
  {
    public ITokenNode ReferenceIdentifier => LongIdentifier?.IdentifierToken;
    public ITokenNode LArrow => null;
  }
}
