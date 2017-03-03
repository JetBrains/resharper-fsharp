using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class TypeAbbreviationPart : FSharpClassLikePart<IFSharpTypeAbbreviationDeclaration>, Class.IClassPart
  {
    public TypeAbbreviationPart(IFSharpTypeAbbreviationDeclaration declaration)
      : base(declaration, declaration.DeclaredName, ModifiersUtil.GetDecoration(declaration.AccessModifiers))
    {
    }

    public TypeAbbreviationPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new Class(this);
    }

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.NONE;
    }

    protected override byte SerializationTag => (byte) FSharpSerializationTag.TypeAbbreviationPart;
  }
}