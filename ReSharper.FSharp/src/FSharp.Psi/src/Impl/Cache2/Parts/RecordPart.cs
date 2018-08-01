using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class RecordPart : RecordPartBase, Class.IClassPart
  {
    public RecordPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
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

  internal class StructRecordPart : RecordPartBase, Struct.IStructPart
  {
    public StructRecordPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    public StructRecordPart(IReader reader) : base(reader)
    {
    }

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.StructRecord;

    public override TypeElement CreateTypeElement() =>
      new FSharpStruct(this);

    public MemberPresenceFlag GetMembersPresenceFlag() =>
      GetMemberPresenceFlag();

    public bool HasHiddenInstanceFields => false;
    public bool IsReadonly => false;
    public bool IsByRefLike => false;
  }

  internal abstract class RecordPartBase : SimpleTypePartBase, IRecordPart
  {
    public bool CliMutable { get; }

    protected RecordPartBase([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder) =>
      CliMutable = declaration.HasAttribute("CLIMutable");

    protected RecordPartBase(IReader reader) : base(reader) =>
      CliMutable = reader.ReadBool();

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteBool(CliMutable);
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

    public IList<ITypeOwner> Fields =>
      GetDeclaration() is IRecordDeclaration recordDeclaration
        ? recordDeclaration.GetFields()
        : EmptyList<ITypeOwner>.Instance;
  }

  public interface IRecordPart : ISimpleTypePart
  {
    IList<ITypeOwner> Fields { get; }
    bool CliMutable { get; }
  }
}
