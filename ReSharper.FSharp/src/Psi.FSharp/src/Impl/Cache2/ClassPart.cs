using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class ClassPart : FSharpTypeMembersOwnerTypePart, Class.IClassPart
  {
    public ClassPart(IFSharpTypeDeclaration declaration, ICacheBuilder cacheBuilder) : base(declaration, cacheBuilder)
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
      // todo: check actual members 
      return MemberPresenceFlag.INSTANCE_CTOR; 
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.Class;
  }
}