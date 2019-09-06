using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ReferenceExpr
  {
    public FSharpIdentifierToken Identifier => IdentifierInternal as FSharpIdentifierToken;
    public override ITokenNode IdentifierToken => IdentifierInternal;

    public string ShortName => Identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public string QualifiedName =>
//      todo: ignore parens for this and qualifier
      Qualifier is IReferenceExpr qualifier && qualifier.QualifiedName is var qualifierName && qualifierName != null
        ? qualifierName + "." + ShortName
        : ShortName;
  }
}
