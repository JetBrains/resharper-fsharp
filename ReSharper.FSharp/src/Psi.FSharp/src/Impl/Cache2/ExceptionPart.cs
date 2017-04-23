using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class ExceptionPart : FSharpClassLikePart<IFSharpExceptionDeclaration>, Class.IClassPart
  {
    private static readonly string[] ourExtendsListShortNames = {"Exception", "IStructuralEquatable"};

    public ExceptionPart(IFSharpExceptionDeclaration declaration, bool isHidden)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifiers), isHidden)
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

    protected override byte SerializationTag => (byte) FSharpPartKind.Exception;

    public override string[] ExtendsListShortNames => ourExtendsListShortNames;
  }
}