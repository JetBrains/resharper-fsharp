using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class AbstractSlot
  {
    public override string DeclaredName => FSharpImplUtil.GetCompiledName(Identifier, Attributes);

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var mfv = GetFSharpSymbol() as FSharpMemberOrFunctionOrValue;
      if (mfv == null) return null;

      // todo: fix getting members in FCS and remove this hack
      var hasDefault = mfv.EnclosingEntity.MembersFunctionsAndValues.Any(m =>
        m.IsOverrideOrExplicitInterfaceImplementation &&
        mfv.LogicalName == m.LogicalName);
      if (hasDefault)
        return null;

      if (mfv.IsProperty || mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod)
      {
        var property = mfv.TryGetPropertyFromAccessor();
        return property != null
          ? new FSharpProperty<AbstractSlot>(this, property)
          : null;
      }

      var typeDeclaration = GetContainingTypeDeclaration() as IFSharpTypeDeclaration;
      return new FSharpMethod<AbstractSlot>(this, mfv, typeDeclaration);
    }
  }
}