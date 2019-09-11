using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ReferenceExpr
  {
    private FSharpSymbolReference myCtorTypeReference;

    public FSharpSymbolReference CtorTypeReference
    {
      get
      {
        if (myCtorTypeReference == null)
        {
          lock (this)
          {
            if (myCtorTypeReference == null)
              myCtorTypeReference = new CtorTypeReference(this);
          }
        }

        return myCtorTypeReference;
      }
    }

    protected override FSharpSymbolReference CreateReference() =>
      new FSharpSymbolReference(this);

    public override ReferenceCollection GetFirstClassReferences() =>
      // todo: workaround array allocation?
      new ReferenceCollection(Reference, CtorTypeReference);

    public override ITokenNode IdentifierToken => Identifier;

    public string ShortName => Identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public string QualifiedName =>
//      todo: ignore parens for this and qualifier
      Qualifier is IReferenceExpr qualifier && qualifier.QualifiedName is var qualifierName && qualifierName != null
        ? qualifierName + "." + ShortName
        : ShortName;
  }
}
