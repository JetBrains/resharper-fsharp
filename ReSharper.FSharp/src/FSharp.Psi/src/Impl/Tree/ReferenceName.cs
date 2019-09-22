using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeReferenceName
  {
    public override ITokenNode IdentifierToken => Identifier as ITokenNode;
    public string ShortName => Identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;
    public string QualifiedName => this.GetQualifiedName();
  }

  internal partial class ExpressionReferenceName
  {
    public override ITokenNode IdentifierToken => Identifier as ITokenNode;

    protected override FSharpSymbolReference CreateReference() =>
      new FSharpSymbolReference(this);

    public string ShortName => Identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;
    public string QualifiedName => this.GetQualifiedName();
  }
}
