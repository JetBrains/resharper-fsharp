using System.Linq;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class AbstractSlot
  {
    public override string DeclaredName => FSharpImplUtil.GetName(Identifier, Attributes);

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var mfv = GetFSharpSymbol() as FSharpMemberOrFunctionOrValue;
      if (mfv != null)
      {
        // todo: fix getting members in FCS and remove this hack
        var hasDefault = mfv.EnclosingEntity.MembersFunctionsAndValues.Any(m =>
          m.IsOverrideOrExplicitInterfaceImplementation &&
          mfv.LogicalName == m.LogicalName);
        if (hasDefault)
          return null;
      }

      if (mfv != null && (mfv.IsProperty || mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod))
        return new FSharpProperty<AbstractSlot>(this, mfv.TryGetPropertyFromAccessor());

      var typeDeclaration = GetContainingTypeDeclaration() as IFSharpTypeDeclaration;
      return new FSharpMethod<AbstractSlot>(this, mfv, typeDeclaration);
    }
  }
}