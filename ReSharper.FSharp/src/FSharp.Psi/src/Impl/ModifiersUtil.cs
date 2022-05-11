using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class ModifiersUtil
  {
    public static MemberDecoration GetDecoration(IUnionCaseDeclaration caseDeclaration)
    {
      if (caseDeclaration.FieldsEnumerable.IsEmpty())
        return MemberDecoration.FromModifiers(Modifiers.INTERNAL);

      return UnionRepresentationNavigator.GetByUnionCase(caseDeclaration) is var repr &&
             FSharpTypeDeclarationNavigator.GetByTypeRepresentation(repr) is { } decl
        ? GetDecoration(decl.AccessModifier, TreeNodeCollection<IAttribute>.Empty)
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

      // todo: use a marker interface to hide the types, as the current approach doesn't work well with IVTs

      if (typePart.GetRoot() is FSharpProjectFilePart { HasPairFile: true })
        // We already know there's no type part in a signature file.
        // If there's a signature file then this type is hidden.
        decoration.AccessRights = AccessRights.INTERNAL;

      if (typePart is ObjectExpressionTypePart or TypeAbbreviationOrDeclarationPartBase { IsUnionCase: false, IsProvidedAndGenerated: false })
        // Type abbreviation is a union case declaration when its right part is a simple named type
        // that is not resolved to anything.
        // When the part is an actual abbreviation, we modify its visibility to hide from other languages.
        // We use the same approach for object expression parts.
        //
        // We cannot set it directly in the part modifiers,
        // since it depends on resolve which needs committed documents and is slow.
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
