using System.Collections.Generic;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Psi.FSharp.Impl
{
  public class ModifiersUtil
  {
    private const string AbstractClass = "AbstractClass";

    public static MemberDecoration GetDecoration(IUnionCaseDeclaration caseDeclaration)
    {
      if (caseDeclaration.FieldsEnumerable.IsEmpty())
        return MemberDecoration.FromModifiers(Modifiers.INTERNAL);

      var unionDeclaration = caseDeclaration.GetContainingTypeDeclaration() as IUnionDeclaration;
      return unionDeclaration != null
        ? GetDecoration(unionDeclaration.AccessModifiers, TreeNodeEnumerable<IFSharpAttribute>.Empty)
        : MemberDecoration.DefaultValue;
    }

    public static MemberDecoration GetDecoration(IAccessModifiers accessModifiers,
      TreeNodeEnumerable<IFSharpAttribute> attributes)
    {
      var decoration = MemberDecoration.DefaultValue;
      var modifiers = new JetHashSet<TokenNodeType>();

      foreach (var modifier in accessModifiers.Modifiers)
        modifiers.Add(modifier.GetTokenType());

      if (modifiers.Contains(FSharpTokenType.PUBLIC)) decoration.Modifiers |= Modifiers.PUBLIC;
      if (modifiers.Contains(FSharpTokenType.INTERNAL)) decoration.Modifiers |= Modifiers.INTERNAL;
      if (modifiers.Contains(FSharpTokenType.PRIVATE)) decoration.Modifiers |= Modifiers.PRIVATE;

      foreach (var attr in attributes)
      {
        var ids = attr.LongIdentifier.Identifiers;
        if (ids.IsEmpty)
          continue;

        if (ids.Last().GetText().SubstringBeforeLast("Attribute") == AbstractClass)
        {
          decoration.Modifiers |= Modifiers.ABSTRACT;
          break;
        }
      }
      return decoration;
    }
  }
}