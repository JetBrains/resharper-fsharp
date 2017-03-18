using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class ClassPart : FSharpObjectModelTypePart, Class.IClassPart
  {
    public ClassPart(IFSharpObjectModelTypeDeclaration declaration) : base(declaration)
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

    protected override byte SerializationTag => (byte) FSharpSerializationTag.ClassPart;
  }
}