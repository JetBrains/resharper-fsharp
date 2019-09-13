using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ReferenceExpr
  {
    public FSharpSymbolReference SymbolReference { get; private set; }
    public FSharpSymbolReference CtorTypeReference { get; private set; }

    private IReference[] myReferences;

    protected override void PreInit()
    {
      base.PreInit();
      SymbolReference = new FSharpSymbolReference(this);
      CtorTypeReference = new TypeReference(this);
      myReferences = new IReference[] {SymbolReference, CtorTypeReference};
    }

    public override ReferenceCollection GetFirstClassReferences() =>
      new ReferenceCollection(myReferences);

    public string ShortName => Identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public string QualifiedName =>
//      todo: ignore parens for this and qualifier
      Qualifier is IReferenceExpr qualifier && qualifier.QualifiedName is var qualifierName && qualifierName != null
        ? qualifierName + "." + ShortName
        : ShortName;

    public override IType Type() =>
      SymbolReference.Resolve().DeclaredElement?.Type() ?? TypeFactory.CreateUnknownType(this);

    public FSharpSymbolReference Reference => SymbolReference;
    public ITokenNode IdentifierToken => Identifier;

    public IFSharpReferenceOwner SetName(string name) =>
      FSharpImplUtil.SetName(this, name);
  }
}
