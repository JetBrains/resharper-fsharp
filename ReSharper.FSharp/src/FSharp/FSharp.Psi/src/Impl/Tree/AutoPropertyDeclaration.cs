using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class AutoPropertyDeclarationStub
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;

    protected override IDeclaredElement CreateDeclaredElement() =>
      GetFcsSymbol() is { } fcsSymbol
        ? CreateDeclaredElement(fcsSymbol)
        : null;

    protected override IDeclaredElement CreateDeclaredElement(FSharpSymbol fcsSymbol)
    {
      if (!(fcsSymbol is FSharpMemberOrFunctionOrValue mfv)) return null;
      if (mfv.IsProperty)
        return new FSharpProperty<IAutoPropertyDeclaration>(this, mfv);

      var property = mfv.AccessorProperty;
      return property != null
        ? new FSharpProperty<IAutoPropertyDeclaration>(this, property.Value)
        : null;
    }

    public override bool IsOverride => this.IsOverride();
    public override bool IsExplicitImplementation => this.IsExplicitImplementation();
  }

  internal class AutoPropertyDeclaration : AutoPropertyDeclarationStub
  {
    public override ITypeUsage SetTypeUsage(ITypeUsage typeUsage)
    {
      if (TypeUsage != null)
        return base.SetTypeUsage(typeUsage);

      var colon = ModificationUtil.AddChildAfter(Identifier, FSharpTokenType.COLON.CreateTreeElement());
      return ModificationUtil.AddChildAfter(colon, typeUsage);
    }
  }
}
