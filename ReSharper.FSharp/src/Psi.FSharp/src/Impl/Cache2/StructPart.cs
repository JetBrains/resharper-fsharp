using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class StructPart : FSharpObjectModelTypePart, Struct.IStructPart
  {
    public StructPart(IFSharpObjectModelTypeDeclaration declaration) : base(declaration)
    {
    }

    public StructPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new Struct(this);
    }

    public MemberPresenceFlag GetMembersPresenceFlag()
    {
      return MemberPresenceFlag.NONE;
    }

    public bool HasHiddenInstanceFields => false; // todo: check this
    protected override byte SerializationTag => (byte) FSharpSerializationTag.StructPart;
  }
}