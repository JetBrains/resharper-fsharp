using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class UnionPart : UnionPartBase, Class.IClassPart
  {
    public UnionPart([NotNull] IUnionDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder,
      bool hasPublicNestedTypes) : base(declaration, cacheBuilder, hasPublicNestedTypes)
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
      bool hasPublicNestedTypes) : base(declaration, cacheBuilder, hasPublicNestedTypes)
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
    public AccessRights RepresentationAccessRights { get; }

    protected UnionPartBase([NotNull] IUnionDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder,
      bool hasPublicNestedTypes) : base(declaration, cacheBuilder)
    {
      HasPublicNestedTypes = hasPublicNestedTypes;
      RepresentationAccessRights = GetRepresentationAccessRights(declaration);
    }

    protected UnionPartBase(IReader reader) : base(reader)
    {
      HasPublicNestedTypes = reader.ReadBool();
      RepresentationAccessRights = (AccessRights) reader.ReadByte();
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteBool(HasPublicNestedTypes);
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

    private static AccessRights GetRepresentationAccessRights([NotNull] IUnionDeclaration declaration)
    {
      var casesList = declaration.UnionCasesList;
      var modifier = casesList.Modifier;
      if (modifier != null)
      {
        var tokenNodeType = modifier.GetTokenType();
        if (tokenNodeType == FSharpTokenType.PRIVATE)
          return AccessRights.PRIVATE;
        if (tokenNodeType == FSharpTokenType.INTERNAL)
          return AccessRights.INTERNAL;
      }

      // todo: hidden by signature in fsi
      return AccessRights.PUBLIC;
    }
  }

  public interface IUnionPart : ISimpleTypePart
  {
    bool HasPublicNestedTypes { get; }
    IList<IUnionCase> Cases { get; }
    AccessRights RepresentationAccessRights { get; }
  }

  public interface IUnionCase : ITypeMember
  {
  }
}
