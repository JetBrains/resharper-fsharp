using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class UnionPart : UnionPartBase, Class.IClassPart
  {
    public UnionPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder, bool hasNestedTypes,
      string[] caseNames) : base(declaration, cacheBuilder, hasNestedTypes, caseNames, PartKind.Class)
    {
    }

    public UnionPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement(IPsiModule module) =>
      new FSharpClass(this);

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.Union;
  }

  internal class StructUnionPart : UnionPartBase, IFSharpStructPart
  {
    public StructUnionPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder,
      string[] caseNames) : base(declaration, cacheBuilder, false, caseNames, PartKind.Struct)
    {
    }

    public StructUnionPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement(IPsiModule module) =>
      new FSharpStruct(this);

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.StructUnion;

    public override IDeclaredType GetBaseClassType() => null;

    public bool IsReadonly => false;
    public bool IsByRefLike => false;
  }

  internal abstract class UnionPartBase : StructuralTypePartBase, IUnionPart
  {
    public bool HasNestedTypes { get; }
    public string[] CaseNames { get; }
    public AccessRights RepresentationAccessRights { get; }

    public virtual bool IsSingleCase => CaseNames.Length == 1;

    protected UnionPartBase([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder,
      bool hasNestedTypes, string[] caseNames, PartKind partKind) : base(declaration, cacheBuilder, partKind)
    {
      HasNestedTypes = hasNestedTypes;
      RepresentationAccessRights = declaration.GetRepresentationAccessRights();
      CaseNames = caseNames;
    }

    protected UnionPartBase(IReader reader) : base(reader)
    {
      HasNestedTypes = reader.ReadBool();
      CaseNames = reader.ReadStringArray();
      RepresentationAccessRights = (AccessRights) reader.ReadByte();
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteBool(HasNestedTypes);
      writer.WriteStringArray(CaseNames);
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

  public interface IFSharpRepresentationAccessRightsOwner
  {
    AccessRights RepresentationAccessRights { get; }
  }

  public interface IUnionPart : IStructuralTypePart, IFSharpRepresentationAccessRightsOwner, IFSharpTypePart
  {
    bool HasNestedTypes { get; }
    bool IsSingleCase { get; }
    string[] CaseNames { get; }
    IList<IUnionCase> Cases { get; }
    TreeNodeCollection<IUnionCaseDeclaration> CaseDeclarations { get; }
  }

  public interface ITypeAbbreviationOrDeclarationPart : IUnionPart
  {
    bool IsUnionCase { get; }
  }
}
