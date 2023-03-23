using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class AutoPropertyDeclaration
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
        return new FSharpProperty<AutoPropertyDeclaration>(this, mfv);

      var property = mfv.AccessorProperty;
      return property != null
        ? new FSharpProperty<AutoPropertyDeclaration>(this, property.Value)
        : null;
    }

    public override bool IsOverride => this.IsOverride();
    public override bool IsExplicitImplementation => this.IsExplicitImplementation();
  }
}
