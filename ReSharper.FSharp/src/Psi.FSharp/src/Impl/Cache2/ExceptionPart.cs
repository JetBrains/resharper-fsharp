using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class ExceptionPart : FSharpClassLikePart<IFSharpExceptionDeclaration>, Class.IClassPart
  {
    public ExceptionPart(IFSharpExceptionDeclaration declaration)
      : base(declaration, declaration.DeclaredName, ModifiersUtil.GetDecoration(declaration.AccessModifiers))
    {
    }

    public ExceptionPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpException(this);
    }

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.NONE;
    }

    protected override byte SerializationTag => (byte) FSharpSerializationTag.ExceptionPart;
  }
}