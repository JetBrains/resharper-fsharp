using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class OpenStatement
  {
    public override ITokenNode IdentifierToken => LongIdentifier?.IdentifierToken;

    protected override FSharpSymbolReference CreateReference() =>
      new OpenStatementReference(this);
  }
}
