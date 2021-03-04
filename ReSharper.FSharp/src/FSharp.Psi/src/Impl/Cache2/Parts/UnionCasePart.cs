using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class UnionCasePart : FSharpClassLikePart<IUnionCaseDeclaration>, IFSharpClassPart,
    IRepresentationAccessRightsOwner
  {
    public UnionCasePart([NotNull] IUnionCaseDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, ModifiersUtil.GetDecoration(declaration),
        TreeNodeCollection<ITypeParameterOfTypeDeclaration>.Empty, cacheBuilder) =>
      ExtendsListShortNames =
        UnionRepresentationNavigator.GetByUnionCase(declaration) is { } repr && 
        FSharpTypeDeclarationNavigator.GetByTypeRepresentation(repr) is { CompiledName: var unionName }
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

    public override IDeclaredType GetBaseClassType() =>
      TypeElement?.GetContainingType() is { } typeElement
        ? TypeFactory.CreateType(typeElement)
        : null;

    public IClass GetSuperClass() => TypeElement?.GetContainingType() as IClass;

    public override IEnumerable<IDeclaredType> GetSuperTypes() =>
      GetBaseClassType() is { } baseType
        ? new[] {baseType}
        : EmptyList<IDeclaredType>.InstanceList;

    public override TypeElement CreateTypeElement() =>
      new FSharpUnionCaseClass(this);

    public override MemberDecoration Modifiers =>
      MemberDecoration.FromModifiers(
        Parent is IUnionPart unionPart &&
        (!unionPart.HasNestedTypes || unionPart.RepresentationAccessRights != AccessRights.PUBLIC)
          ? ReSharper.Psi.Modifiers.INTERNAL
          : ReSharper.Psi.Modifiers.PUBLIC);

    public IUnionCase UnionCase =>
      (IUnionCase) ((ITypeMemberDeclaration) GetDeclaration())?.DeclaredElement;

    public override MemberPresenceFlag GetMemberPresenceFlag() =>
      MemberPresenceFlag.INSTANCE_CTOR;

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.UnionCase;

    public AccessRights RepresentationAccessRights =>
      Parent is IUnionPart unionPart
        ? unionPart.RepresentationAccessRights
        : AccessRights.PUBLIC;
  }
}
