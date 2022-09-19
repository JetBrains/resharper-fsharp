using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class FSharpClassLikePart<T> : FSharpTypeParametersOwnerPart<T>, IFSharpClassLikePart
    where T : class, IFSharpTypeOldDeclaration
  {
    private readonly MemberPresenceFlag myMembersMask;

    protected FSharpClassLikePart([NotNull] T declaration, MemberDecoration memberDecoration,
      IList<ITypeParameterDeclaration> typeParameters, [NotNull] ICacheBuilder cacheBuilder, PartKind partKind)
      : base(declaration, memberDecoration, typeParameters, cacheBuilder)
    {
      myMembersMask = InvestigateMembers(declaration, partKind);
    }

    protected FSharpClassLikePart(IReader reader) : base(reader)
    {
      myMembersMask = (MemberPresenceFlag) reader.ReadUShort();
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteUShort((ushort) myMembersMask);
    }

    public virtual IEnumerable<ITypeMember> GetTypeMembers()
    {
      var declaration = GetDeclaration();
      if (declaration == null)
        return EmptyList<ITypeMember>.Instance;

      var result = new LocalList<ITypeMember>();
      foreach (var memberDeclaration in declaration.MemberDeclarations)
      {
        var declaredElement = memberDeclaration.DeclaredElement;
        if (declaredElement != null)
          result.Add(declaredElement);
      }

      return result.ResultingList();
    }

    public abstract IEnumerable<IDeclaredType> GetSuperTypes();
    public virtual IEnumerable<ITypeElement> GetSuperTypeElements() => GetSuperTypes().AsIList().ToTypeElements();

    public virtual MemberPresenceFlag GetMemberPresenceFlag() => myMembersMask;

    private static MemberPresenceFlag InvestigateMembers([NotNull] T declaration, PartKind partKind)
    {
      MemberPresenceFlag membersMask = 0;

      var isInterface = partKind == PartKind.Interface;

      foreach (var decl in declaration.MemberDeclarations)
      {
        var compiledName = decl.DeclaredName;
        if (compiledName.StartsWith("op_", StringComparison.Ordinal))
        {
          switch (compiledName)
          {
            case StandardOperatorNames.Explicit:
              membersMask |= MemberPresenceFlag.EXPLICIT_OP;
              break;
            case StandardOperatorNames.Implicit:
              membersMask |= MemberPresenceFlag.IMPLICIT_OP;
              break;
            default:
              membersMask |= MemberPresenceFlag.SIGN_OR_EQUALITY_OP;
              break;
          }
        }

        if (decl is IOverridableMemberDeclaration overridableDecl)
        {
          if (compiledName == "Equals" && overridableDecl.IsOverride && 
              overridableDecl.GetAccessRights() == AccessRights.PUBLIC)
          {
            membersMask |= MemberPresenceFlag.MAY_EQUALS_OVERRIDE;
          }

          if (isInterface && overridableDecl.IsStatic && overridableDecl.IsAbstract)
            membersMask |= MemberPresenceFlag.HAS_STATIC_ABSTRACT_MEMBERS;
        }

        if (decl is IConstructorSignatureOrDeclaration constructorDeclOrSig &&
            constructorDeclOrSig.GetAccessRights() == AccessRights.PUBLIC)
        {
          membersMask |= MemberPresenceFlag.INSTANCE_CTOR;

          if (constructorDeclOrSig is IConstructorDeclaration constructorDecl)
          {
            if (constructorDecl.ParameterPatterns.IgnoreInnerParens() is IUnitPat)
              membersMask |= MemberPresenceFlag.PUBLIC_DEFAULT_CTOR;
          }
          
          else if (constructorDeclOrSig is IConstructorSignature constructorSig)
          {
            if (constructorSig.ReturnTypeInfo?.ReturnType is IFunctionTypeUsage funTypeUsage && 
                funTypeUsage.ArgumentTypeUsage.IgnoreInnerParens() is INamedTypeUsage { ReferenceName: { ShortName: "unit" } } &&
                funTypeUsage.ReturnTypeUsage.IgnoreInnerParens() is INamedTypeUsage namedTypeUsage && 
                namedTypeUsage.ReferenceName?.ShortName == declaration.SourceName)
            {
              membersMask |= MemberPresenceFlag.PUBLIC_DEFAULT_CTOR;
            }
          }
        }
      }

      return membersMask;
    }

    public virtual IDeclaredType GetBaseClassType() =>
      ExtendsListShortNames.IsEmpty()
        ? null
        : GetDeclaration()?.BaseClassType ?? GetPsiModule().GetPredefinedType().Object;
  }
}
