using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class TypeAbbreviationPart : FSharpClassLikePart<IFSharpTypeAbbreviationDeclaration>, Class.IClassPart
  {
    public TypeAbbreviationPart(IFSharpTypeAbbreviationDeclaration declaration)
      : base(declaration, declaration.DeclaredName, MemberDecoration.DefaultValue)
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

    public override MemberDecoration Modifiers =>
      // workaround for types not presented in compiled code
      MemberDecoration.FromModifiers(Psi.Modifiers.INTERNAL);

    protected override byte SerializationTag => (byte) FSharpSerializationTag.TypeAbbreviationPart;
  }
}