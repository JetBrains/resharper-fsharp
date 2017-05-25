using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class ExceptionPart : FSharpTypeMembersOwnerTypePart, Class.IClassPart
  {
    private static readonly string[] ourExtendsListShortNames = {"Exception", "IStructuralEquatable"};

    public ExceptionPart(IFSharpTypeDeclaration declaration, ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
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

    public override string[] ExtendsListShortNames =>
      ArrayUtil.Add(ourExtendsListShortNames, base.ExtendsListShortNames);
  }
}