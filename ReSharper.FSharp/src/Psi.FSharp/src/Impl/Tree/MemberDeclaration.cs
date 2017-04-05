using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class MemberDeclaration
  {
    public override string DeclaredName => FSharpImplUtil.GetName(Identifier, Attributes);

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var memberParams = Parameters;

      var mfv = GetFSharpSymbol() as FSharpMemberOrFunctionOrValue;
      if (memberParams.IsEmpty)
      {
        var entityMembers = mfv?.EnclosingEntity.MembersFunctionsAndValues;
        var property = entityMembers?.SingleItem(m => m.IsProperty && !m.IsPropertyGetterMethod &&
                                                      !m.IsPropertySetterMethod && m.DisplayName == mfv.DisplayName);
        return new FSharpProperty(this, property);
      }

      var typeDeclaration = GetContainingTypeDeclaration() as IFSharpTypeParametersOwnerDeclaration;
      return new FSharpMethod(this, mfv, typeDeclaration);
    }
  }
}