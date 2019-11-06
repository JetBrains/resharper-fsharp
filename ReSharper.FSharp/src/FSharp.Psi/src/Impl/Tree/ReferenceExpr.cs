using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
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
      SymbolReference = null;
      CtorTypeReference = null;
      myReferences = null;
    }

    public override ReferenceCollection GetFirstClassReferences()
    {
      if (myReferences == null)
      {
        lock (this)
        {
          if (myReferences == null)
          {
            if (SymbolReference == null)
              SymbolReference = new FSharpSymbolReference(this);

            var appExpr = PrefixAppExprNavigator.GetByFunctionExpression(this.IgnoreParentParens());
            if (appExpr == null)
            {
              myReferences = new IReference[] {SymbolReference};
            }
            else
            {
              CtorTypeReference = new ReferenceExpressionTypeReference(this);
              myReferences = new IReference[] {SymbolReference, CtorTypeReference};
            }
          }
        }
      }

      return new ReferenceCollection(myReferences);
    }

    public string ShortName => Identifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public string QualifiedName =>
//      todo: ignore parens for this and qualifier
      Qualifier is IReferenceExpr qualifier && qualifier.QualifiedName is var qualifierName && qualifierName != null
        ? qualifierName + "." + ShortName
        : ShortName;

    public override IType Type() =>
      Reference.Resolve().DeclaredElement?.Type() ?? TypeFactory.CreateUnknownType(this);

    public FSharpSymbolReference Reference
    {
      get
      {
        if (SymbolReference != null)
          return SymbolReference;

        lock (this)
        {
          if (SymbolReference == null)
            SymbolReference = new FSharpSymbolReference(this);
        }
        return SymbolReference;
      }
    }

    public ITokenNode IdentifierToken => Identifier as ITokenNode;

    public IFSharpReferenceOwner SetName(string name) =>
      FSharpImplUtil.SetName(this, name);
  }

  public class ReferenceExpressionTypeReference : FSharpSymbolReference
  {
    public ReferenceExpressionTypeReference([NotNull] IFSharpReferenceOwner owner) : base(owner)
    {
    }

    public override FSharpSymbol GetFSharpSymbol()
    {
      if (base.GetFSharpSymbol() is FSharpMemberOrFunctionOrValue mfv && mfv.IsConstructor)
        return mfv.DeclaringEntity?.Value;

      return null;
    }
  }
}
