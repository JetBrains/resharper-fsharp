using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class UnionCasePart : FSharpClassLikePart<INestedTypeUnionCaseDeclaration>, Class.IClassPart,
    IRepresentationAccessRightsOwner
  {
    public UnionCasePart([NotNull] INestedTypeUnionCaseDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, ModifiersUtil.GetDecoration(declaration),
        TreeNodeCollection<ITypeParameterOfTypeDeclaration>.Empty, cacheBuilder) =>
      ExtendsListShortNames =
        declaration.GetContainingNode<IUnionDeclaration>()?.CompiledName is var unionName && unionName != null
          ? new[] {cacheBuilder.Intern(unionName)}
          : EmptyArray<string>.Instance;

    public UnionCasePart(IReader reader) : base(reader) =>
      ExtendsListShortNames = reader.ReadStringArray();

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteStringArray(ExtendsListShortNames);
    }

    public override string[] ExtendsListShortNames { get; }

    public IDeclaredType GetBaseClassType() =>
      TypeElement?.GetContainingType() is ITypeElement typeElement
        ? TypeFactory.CreateType(typeElement)
        : null;

    public override IEnumerable<IDeclaredType> GetSuperTypes() =>
      GetBaseClassType() is IDeclaredType baseType
        ? new[] {baseType}
        : EmptyList<IDeclaredType>.InstanceList;

    public override TypeElement CreateTypeElement() =>
      new FSharpNestedTypeUnionCase(this);

    public override MemberDecoration Modifiers =>
      MemberDecoration.FromModifiers(
        Parent is IUnionPart unionPart &&
        (!unionPart.HasPublicNestedTypes || unionPart.RepresentationAccessRights != AccessRights.PUBLIC)
          ? ReSharper.Psi.Modifiers.INTERNAL
          : ReSharper.Psi.Modifiers.PUBLIC);

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.INSTANCE_CTOR;
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.UnionCase;

    public IList<FSharpUnionCaseField<UnionCaseFieldDeclaration>> CaseFields
    {
      get
      {
        var declaration = GetDeclaration();
        if (declaration == null)
          return EmptyList<FSharpUnionCaseField<UnionCaseFieldDeclaration>>.Instance;

        var result = new LocalList<FSharpUnionCaseField<UnionCaseFieldDeclaration>>();
        foreach (var fieldDeclaration in declaration.Fields)
        {
          if (fieldDeclaration.DeclaredElement is FSharpUnionCaseField<UnionCaseFieldDeclaration> field)
            result.Add(field);
        }

        return result.ResultingList();
      }
    }

    public AccessRights RepresentationAccessRights =>
      Parent is IUnionPart unionPart
        ? unionPart.RepresentationAccessRights
        : AccessRights.PUBLIC;
  }
}
