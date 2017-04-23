using System.Linq;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.FSharp.Tree;
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
      var mfv = GetFSharpSymbol() as FSharpMemberOrFunctionOrValue;
      if (mfv != null && (mfv.IsProperty || mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod))
      {
        var entityMembers = mfv.EnclosingEntity.MembersFunctionsAndValues; //todo inheritance with same name
        // same trouble as with constructors: member from entity (and its type) differs from initial
        var mfvReturnTypeString = mfv.ReturnParameter.Type.ToString();
        var property = entityMembers?
          .FirstOrDefault(m => m.IsProperty && !m.IsPropertyGetterMethod && !m.IsPropertySetterMethod &&
                               m.DisplayName == mfv.DisplayName &&
                               m.ReturnParameter.Type.ToString() == mfvReturnTypeString);
        return new FSharpProperty<MemberDeclaration>(this, property);
      }

      var typeDeclaration = GetContainingTypeDeclaration() as IFSharpTypeParametersOwnerDeclaration;
      return new FSharpMethod(this, mfv, typeDeclaration);
    }
  }
}