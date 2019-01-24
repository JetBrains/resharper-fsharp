using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class AbstractSlot
  {
    protected override string DeclaredElementName => Identifier.GetCompiledName(Attributes);

    public override IFSharpIdentifier NameIdentifier => Identifier;

    protected override IDeclaredElement CreateDeclaredElement()
    {
      if (!(GetFSharpSymbol() is FSharpMemberOrFunctionOrValue mfv)) return null;

      // todo: remove this and provide API in FCS and cache it somehow
      var hasDefault = mfv.DeclaringEntity?.Value.MembersFunctionsAndValues.Any(m =>
                         m.IsOverrideOrExplicitInterfaceImplementation &&
                         mfv.LogicalName == m.LogicalName) ?? false;
      if (hasDefault)
        return null;

      if (mfv.IsProperty)
        return new FSharpProperty<AbstractSlot>(this, mfv);

      var property = mfv.AccessorProperty;
      if (property != null)
        return new FSharpProperty<AbstractSlot>(this, property.Value);

      var typeDeclaration = GetContainingTypeDeclaration() as IFSharpTypeDeclaration;
      return new FSharpMethod<AbstractSlot>(this, mfv, typeDeclaration);
    }
  }
}