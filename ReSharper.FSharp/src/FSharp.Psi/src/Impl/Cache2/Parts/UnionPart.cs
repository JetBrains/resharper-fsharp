using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class UnionPart : UnionPartBase, Class.IClassPart
  {
    public UnionPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder, bool hasNestedTypes,
      bool isSingleCase) : base(declaration, cacheBuilder, hasNestedTypes, isSingleCase)
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

    public override IDeclaredType GetBaseClassType() => null;

    public bool IsReadonly => false;
    public bool IsByRefLike => false;
  }

  internal abstract class UnionPartBase : SimpleTypePartBase, IUnionPart
  {
    public bool HasNestedTypes { get; }
    public bool IsSingleCase { get; }
    public AccessRights RepresentationAccessRights { get; }

    protected UnionPartBase([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder,
      bool hasNestedTypes, bool isSingleCase) : base(declaration, cacheBuilder)
    {
      HasNestedTypes = hasNestedTypes;
      RepresentationAccessRights = declaration.GetRepresentationAccessRights();
      IsSingleCase = isSingleCase;
    }

    protected UnionPartBase(IReader reader) : base(reader)
    {
      HasNestedTypes = reader.ReadBool();
      IsSingleCase = reader.ReadBool();
      RepresentationAccessRights = (AccessRights) reader.ReadByte();
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteBool(HasNestedTypes);
      writer.WriteBool(IsSingleCase);
      writer.WriteByte((byte) RepresentationAccessRights);
    }

    public IList<IUnionCase> Cases
    {
      get
      {
        if (!(GetDeclaration() is IFSharpTypeDeclaration { TypeRepresentation: IUnionRepresentation repr }))
          return EmptyList<IUnionCase>.Instance;

        var result = new LocalList<IUnionCase>();
        foreach (var memberDeclaration in repr.UnionCases)
          if (((ITypeMemberDeclaration) memberDeclaration).DeclaredElement is IUnionCase unionCase)
            result.Add(unionCase);

        return result.ResultingList();
      }
    }

    public TreeNodeCollection<IUnionCaseDeclaration> CaseDeclarations =>
      GetDeclaration() is IFSharpTypeDeclaration { TypeRepresentation: IUnionRepresentation repr }
        ? repr.UnionCases
        : TreeNodeCollection<IUnionCaseDeclaration>.Empty;


    public override IEnumerable<ITypeMember> GetTypeMembers()
    {
      if (HasNestedTypes || !(GetDeclaration() is IFSharpTypeDeclaration { TypeRepresentation: IUnionRepresentation repr }))
        return base.GetTypeMembers();

      var fields = new FrugalLocalList<ITypeMember>();

      foreach (var caseDecl in repr.UnionCasesEnumerable)
        foreach (var fieldDeclaration in caseDecl.FieldsEnumerable)
          if (fieldDeclaration.DeclaredElement is { } field)
            fields.Add(field);

      return fields.ResultingList().Prepend(base.GetTypeMembers());
    }
  }

  public interface IRepresentationAccessRightsOwner
  {
    AccessRights RepresentationAccessRights { get; }
  }

  public interface IUnionPart : ISimpleTypePart, IRepresentationAccessRightsOwner
  {
    bool HasNestedTypes { get; }
    bool IsSingleCase { get; }
    IList<IUnionCase> Cases { get; }
    TreeNodeCollection<IUnionCaseDeclaration> CaseDeclarations { get; }
  }
}
