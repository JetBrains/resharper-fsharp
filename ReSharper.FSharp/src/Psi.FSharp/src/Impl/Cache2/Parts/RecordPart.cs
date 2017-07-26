using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Parts
{
  internal class RecordPart : SimpleTypePartBase, Class.IClassPart
  {
    public RecordPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    public RecordPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpRecord(this);
    }

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.INSTANCE_CTOR;
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.Record;
  }
}