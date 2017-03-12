using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class RecordPart : FSharpClassLikePart<IFSharpRecordDeclaration>, Class.IClassPart
  {
    private static readonly string[] ourExtendsListShortNames =
      {"IStructuralEquatable", "IStructuralComparable", "IComparable"};

    public RecordPart(IFSharpRecordDeclaration declaration) : base(declaration, declaration.DeclaredName,
      ModifiersUtil.GetDecoration(declaration.AccessModifiers), declaration.TypeParameters.Count)
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

    protected override byte SerializationTag => (byte) FSharpSerializationTag.RecordPart;

    public override string[] ExtendsListShortNames => ourExtendsListShortNames;
  }
}