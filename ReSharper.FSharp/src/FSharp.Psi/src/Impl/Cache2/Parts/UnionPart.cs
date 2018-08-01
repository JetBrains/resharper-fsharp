using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class UnionPart : UnionPartBase, Class.IClassPart
  {
    public UnionPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder,
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
    public StructUnionPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder,
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

    protected UnionPartBase([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder,
      bool hasPublicNestedTypes) : base(declaration, cacheBuilder) =>
      HasPublicNestedTypes = hasPublicNestedTypes;

    protected UnionPartBase(IReader reader) : base(reader) =>
      HasPublicNestedTypes = reader.ReadBool();

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteBool(HasPublicNestedTypes);
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
  }

  public interface IUnionPart : ISimpleTypePart
  {
    bool HasPublicNestedTypes { get; }
    IList<IUnionCase> Cases { get; }
  }

  public interface IUnionCase : ITypeMember
  {
  }
}
