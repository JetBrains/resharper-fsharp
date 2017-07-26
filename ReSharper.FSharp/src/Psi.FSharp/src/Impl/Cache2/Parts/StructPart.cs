using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Parts
{
  internal class StructPart : FSharpTypeMembersOwnerTypePart, Struct.IStructPart
  {
    public StructPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    public StructPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpStruct(this);
    }

    public MemberPresenceFlag GetMembersPresenceFlag()
    {
      return MemberPresenceFlag.NONE;
    }

    public bool HasHiddenInstanceFields => false; // todo: check this
    protected override byte SerializationTag => (byte) FSharpPartKind.Struct;
  }

  public class FSharpStruct : Struct
  {
    public FSharpStruct([NotNull] IStructPart part) : base(part)
    {
    }

    protected override MemberDecoration Modifiers => myParts.GetModifiers();
  }
}