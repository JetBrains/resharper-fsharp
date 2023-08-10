using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class RecordPart : RecordPartBase, Class.IClassPart
  {
    public RecordPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder, PartKind.Class)
    {
    }

    public RecordPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpClass(this);

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.Record;
  }

  internal class StructRecordPart : RecordPartBase, IFSharpStructPart
  {
    public StructRecordPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder, PartKind.Struct)
    {
    }

    public StructRecordPart(IReader reader) : base(reader)
    {
    }

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.StructRecord;

    public override TypeElement CreateTypeElement() =>
      new FSharpStruct(this);

    public override IDeclaredType GetBaseClassType() => null;

    public override IEnumerable<ITypeMember> GetTypeMembers()
    {
      var typeElement = TypeElement;
      return CliMutable && typeElement != null
        ? FixedList.Of(new DefaultConstructor(typeElement)).Concat(base.GetTypeMembers())
        : base.GetTypeMembers();
    }

    public bool IsReadonly => false;
    public bool IsByRefLike => false;
  }

  internal abstract class RecordPartBase : StructuralTypePartBase, IRecordPart, IGeneratedConstructorOwner
  {
    public bool CliMutable { get; }
    public AccessRights RepresentationAccessRights { get; }

    protected RecordPartBase([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder, 
      PartKind partKind)
      : base(declaration, cacheBuilder, partKind)
    {
      CliMutable = declaration.Attributes.HasAttribute("CLIMutable");
      RepresentationAccessRights = declaration.GetRepresentationAccessRights();
    }

    protected RecordPartBase(IReader reader) : base(reader)
    {
      CliMutable = reader.ReadBool();
      RepresentationAccessRights = (AccessRights) reader.ReadByte();
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteBool(CliMutable);
      writer.WriteByte((byte) RepresentationAccessRights);
    }

    public override MemberDecoration Modifiers
    {
      get
      {
        var modifiers = base.Modifiers;
        modifiers.IsSealed = true;
        return modifiers;
      }
    }

    public override MemberPresenceFlag GetMemberPresenceFlag() =>
      CliMutable
        ? base.GetMemberPresenceFlag() | MemberPresenceFlag.PUBLIC_DEFAULT_CTOR
        : base.GetMemberPresenceFlag();

    public IList<ITypeOwner> Fields =>
      GetDeclaration() is IFSharpTypeDeclaration { TypeRepresentation: IRecordRepresentation repr }
        ? repr.GetFields()
        : EmptyList<ITypeOwner>.Instance;

    public IParametersOwner GetConstructor() =>
      new FSharpGeneratedConstructorFromFields(this);
  }

  public interface IRecordPart : IFieldsOwnerPart, IRepresentationAccessRightsOwner, IFSharpTypePart
  {
    bool CliMutable { get; }
  }

  public interface IFieldsOwnerPart : IStructuralTypePart
  {
    IList<ITypeOwner> Fields { get; }
  }
}
