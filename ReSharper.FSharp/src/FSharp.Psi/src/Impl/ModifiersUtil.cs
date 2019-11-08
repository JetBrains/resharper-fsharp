using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class ModifiersUtil
  {
    public static MemberDecoration GetDecoration(INestedTypeUnionCaseDeclaration caseDeclaration)
    {
      if (caseDeclaration.FieldsEnumerable.IsEmpty())
        return MemberDecoration.FromModifiers(Modifiers.INTERNAL);

      return caseDeclaration.GetContainingTypeDeclaration() is IUnionDeclaration unionDeclaration
        ? GetDecoration(unionDeclaration.AccessModifier, TreeNodeCollection<IAttribute>.Empty)
        : MemberDecoration.DefaultValue;
    }

    public static MemberDecoration GetDecoration([CanBeNull] ITokenNode accessModifier,
      TreeNodeCollection<IAttribute> attributes)
    {
      var decoration = MemberDecoration.DefaultValue;
      if (accessModifier != null)
      {
        var tokenType = accessModifier.GetTokenType();
        if (tokenType == FSharpTokenType.INTERNAL)
          decoration.Modifiers |= Modifiers.INTERNAL;
        else if (tokenType == FSharpTokenType.PRIVATE)
          decoration.Modifiers |= Modifiers.PRIVATE;
        else if (tokenType == FSharpTokenType.PUBLIC)
          decoration.Modifiers |= Modifiers.PUBLIC;
      }

      foreach (var attr in attributes)
      {
        switch (attr.GetShortName())
        {
          case FSharpImplUtil.AbstractClass:
            decoration.Modifiers |= Modifiers.ABSTRACT;
            break;
          case FSharpImplUtil.Sealed:
            decoration.Modifiers |= Modifiers.SEALED;
            break;
        }
      }

      return Normalize(decoration);
    }

    public static MemberDecoration GetModifiers(this TypePart typePart)
    {
      var sigPart = GetPartFromSignature(typePart);
      if (sigPart != null)
        return Normalize(sigPart.Modifiers);

      if (typePart == null)
        return MemberDecoration.DefaultValue;

      var decoration = typePart.Modifiers;
      var isHiddenBySignature = (typePart.GetRoot() as FSharpProjectFilePart)?.HasPairFile ?? false;
      if (isHiddenBySignature)
        decoration.AccessRights = AccessRights.INTERNAL;

      return Normalize(decoration);
    }

    private static MemberDecoration Normalize(MemberDecoration decoration)
    {
      if (decoration.AccessRights == AccessRights.NONE)
        decoration.AccessRights = AccessRights.PUBLIC;

      if (decoration.IsStatic)
      {
        decoration.IsAbstract = true;
        decoration.IsSealed = true;
      }

      return decoration;
    }

    [CanBeNull]
    private static TypePart GetPartFromSignature(TypePart typePart)
    {
      for (var part = typePart; part != null; part = part.NextPart)
      {
        var filePart = part.GetRoot() as FSharpProjectFilePart;
        if (filePart?.IsSignaturePart ?? false)
          return part;
      }

      return null;
    }

    public static AccessRights GetAccessRights([CanBeNull] ITokenNode accessModifier)
    {
      var modifierTokenType = accessModifier?.GetTokenType();
      if (modifierTokenType == FSharpTokenType.PRIVATE)
        return AccessRights.PRIVATE;
      if (modifierTokenType == FSharpTokenType.INTERNAL)
        return AccessRights.INTERNAL;

      return AccessRights.PUBLIC;
    }
  }
}
