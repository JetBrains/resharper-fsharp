using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

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
            SymbolReference ??= new FSharpSymbolReference(this);

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
      Qualifier is IReferenceExpr { QualifiedName: { } qualifierName }
        ? qualifierName + "." + ShortName
        : ShortName;

    public FSharpSymbolReference Reference
    {
      get
      {
        if (SymbolReference != null)
          return SymbolReference;

        lock (this)
          SymbolReference ??= new FSharpSymbolReference(this);

        return SymbolReference;
      }
    }

    public IFSharpIdentifier FSharpIdentifier => Identifier;

    public IFSharpReferenceOwner SetName(string name) =>
      FSharpImplUtil.SetName(this, name);

    ITypeArgumentList ITypeArgumentOwner.TypeArgumentList => TypeArgumentList;

    public bool IsQualified => Qualifier != null;

    public FSharpSymbolReference QualifierReference =>
      Qualifier is IReferenceExpr refExpr ? refExpr.Reference : null;

    public void SetQualifier(IClrDeclaredElement declaredElement)
    {
      // todo: implement for existing qualifiers
      Assertion.Assert(Qualifier == null, "Qualifier == null");
      this.SetQualifier(this.CreateElementFactory().CreateReferenceExpr, declaredElement);
    }

    public IList<string> Names => this.GetNames();
  }

  public class ReferenceExpressionTypeReference : FSharpSymbolReference
  {
    public ReferenceExpressionTypeReference([NotNull] IFSharpReferenceOwner owner) : base(owner)
    {
    }

    public override FSharpSymbol GetFSharpSymbol() =>
      base.GetFSharpSymbol() switch
      {
        FSharpMemberOrFunctionOrValue { IsConstructor: true } mfv => mfv.DeclaringEntity?.Value,
        // FSharpUnionCase unionCase => unionCase.ReturnType.TypeDefinition,
        _ => null
      };
  }
}
