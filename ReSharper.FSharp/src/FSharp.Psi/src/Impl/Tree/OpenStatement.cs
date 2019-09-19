using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class OpenStatement
  {
    public override ITokenNode IdentifierToken => ReferenceName?.Identifier as ITokenNode;
  }
}
