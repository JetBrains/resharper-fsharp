using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class OpenStatement
  {
    public override ITokenNode IdentifierToken => ImportedName?.Identifier;

    protected override FSharpSymbolReference CreateReference() =>
      new OpenStatementReference(this);
  }
}
