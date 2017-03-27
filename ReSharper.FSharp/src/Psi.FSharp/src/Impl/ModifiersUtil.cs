using System.Collections.Generic;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Psi.FSharp.Impl
{
  public class ModifiersUtil
  {
    public static MemberDecoration GetDecoration(IFSharpUnionCaseDeclaration caseDeclaration)
    {
      if (caseDeclaration.FieldsEnumerable.IsEmpty())
        return MemberDecoration.FromModifiers(Modifiers.INTERNAL);

      var unionDeclaration = caseDeclaration.GetContainingTypeDeclaration() as IFSharpUnionDeclaration;
      return unionDeclaration != null
        ? GetDecoration(unionDeclaration.AccessModifiers)
        : MemberDecoration.DefaultValue;
    }

    public static MemberDecoration GetDecoration(IAccessModifiers accessModifiers)
    {
      var decoration = MemberDecoration.DefaultValue;
      var modifiers = new JetHashSet<TokenNodeType>();

      foreach (var modifier in accessModifiers.Modifiers)
        modifiers.Add(modifier.GetTokenType());

      if (modifiers.Contains(FSharpTokenType.PUBLIC)) decoration.Modifiers |= Modifiers.PUBLIC;
      if (modifiers.Contains(FSharpTokenType.INTERNAL)) decoration.Modifiers |= Modifiers.INTERNAL;
      if (modifiers.Contains(FSharpTokenType.PRIVATE)) decoration.Modifiers |= Modifiers.PRIVATE;

      return decoration;
    }
  }
}