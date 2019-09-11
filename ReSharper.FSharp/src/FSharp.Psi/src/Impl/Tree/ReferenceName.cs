using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeReferenceName
  {
    public override ITokenNode IdentifierToken => Identifier;
    public string ShortName => Identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;
  }

  internal partial class ExpressionReferenceName
  {
    public override ITokenNode IdentifierToken => Identifier;

    protected override FSharpSymbolReference CreateReference() =>
      new FSharpSymbolReference(this);

    public string ShortName => Identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;
  }
}
