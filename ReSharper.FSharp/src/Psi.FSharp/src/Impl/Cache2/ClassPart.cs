using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class ClassPart : FSharpClassLikePart<IFSharpObjectModelTypeDeclaration>, Class.IClassPart
  {
    public ClassPart(IFSharpObjectModelTypeDeclaration declaration)
      : base(declaration, declaration.DeclaredName, MemberDecoration.DefaultValue)
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
      return MemberPresenceFlag.NONE; // todo: check members for this
    }

    protected override byte SerializationTag => (byte) FSharpSerializationTag.ClassPart;
  }
}