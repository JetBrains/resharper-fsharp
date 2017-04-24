using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class AutoProperty
  {
    public override string DeclaredName => FSharpImplUtil.GetCompiledName(Identifier, Attributes);

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var mfv = GetFSharpSymbol() as FSharpMemberOrFunctionOrValue;
      var entityMembers = mfv?.EnclosingEntity.MembersFunctionsAndValues; //todo inheritance with same name
      var property = entityMembers?.SingleItem(m => m.IsProperty && !m.IsPropertyGetterMethod &&
                                                    !m.IsPropertySetterMethod && m.DisplayName == mfv.DisplayName);
      return new FSharpProperty<AutoProperty>(this, property);
    }
  }
}