using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class ClassPart : FSharpTypeMembersOwnerTypePart, Class.IClassPart
  {
    public ClassPart(IFSharpTypeDeclaration declaration, bool isHidden) : base(declaration, isHidden)
    {
    }

    public ClassPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new Class(this);
    }

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.INSTANCE_CTOR; // todo: check members for this
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.Class;
  }
}