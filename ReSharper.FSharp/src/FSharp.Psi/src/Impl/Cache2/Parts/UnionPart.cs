using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class UnionPart : UnionPartBase, Class.IClassPart
  {
    public UnionPart([NotNull] IUnionDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder,
      bool hasPublicNestedTypes, bool isSingleCase) : base(declaration, cacheBuilder, hasPublicNestedTypes, isSingleCase)
    {
    }

    public UnionPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpClass(this);

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.Union;
  }

  internal class StructUnionPart : UnionPartBase, Struct.IStructPart
  {
    public StructUnionPart([NotNull] IUnionDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder,
      bool isSingleCase) : base(declaration, cacheBuilder, false, isSingleCase)
    {
    }

    public StructUnionPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpStruct(this);

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.StructUnion;

    public MemberPresenceFlag GetMembersPresenceFlag() =>
      GetMemberPresenceFlag();

    public bool HasHiddenInstanceFields => false;
    public bool IsReadonly => false;
    public bool IsByRefLike => false;
  }

  internal abstract class UnionPartBase : SimpleTypePartBase, IUnionPart
  {
    public bool HasPublicNestedTypes { get; }
    public bool IsSingleCaseUnion { get; }
    public AccessRights RepresentationAccessRights { get; }

    protected UnionPartBase([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder,
      bool hasPublicNestedTypes, bool isSingleCaseUnion) : base(declaration, cacheBuilder)
    {
      HasPublicNestedTypes = hasPublicNestedTypes;
      RepresentationAccessRights = GetRepresentationAccessRights(declaration);
      IsSingleCaseUnion = isSingleCaseUnion;
    }

    protected UnionPartBase(IReader reader) : base(reader)
    {
      HasPublicNestedTypes = reader.ReadBool();
      IsSingleCaseUnion = reader.ReadBool();
      RepresentationAccessRights = (AccessRights) reader.ReadByte();
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteBool(HasPublicNestedTypes);
      writer.WriteBool(IsSingleCaseUnion);
      writer.WriteByte((byte) RepresentationAccessRights);
    }

    public IList<IUnionCase> Cases
    {
      get
      {
        if (!(GetDeclaration() is IUnionDeclaration unionDeclaration))
          return EmptyList<IUnionCase>.Instance;

        var result = new LocalList<IUnionCase>();
        foreach (var memberDeclaration in unionDeclaration.UnionCases)
          if (memberDeclaration.DeclaredElement is IUnionCase unionCase)
            result.Add(unionCase);

        return result.ResultingList();
      }
    }

    public TreeNodeCollection<IUnionCaseDeclaration> CaseDeclarations =>
      GetDeclaration() is IUnionDeclaration declaration
        ? declaration.UnionCases
        : TreeNodeCollection<IUnionCaseDeclaration>.Empty;

    // todo: hidden by signature in fsi
    private static AccessRights GetRepresentationAccessRights([NotNull] IFSharpTypeDeclaration declaration) =>
      declaration is IUnionDeclaration unionDeclaration
        ? ModifiersUtil.GetAccessRights(unionDeclaration.UnionRepresentation.AccessModifier)
        : AccessRights.PUBLIC;
  }

  public interface IRepresentationAccessRightsOwner
  {
    AccessRights RepresentationAccessRights { get; }
  }

  public interface IUnionPart : ISimpleTypePart, IRepresentationAccessRightsOwner
  {
    bool HasPublicNestedTypes { get; }
    bool IsSingleCaseUnion { get; }
    IList<IUnionCase> Cases { get; }
    TreeNodeCollection<IUnionCaseDeclaration> CaseDeclarations { get; }
  }
}
