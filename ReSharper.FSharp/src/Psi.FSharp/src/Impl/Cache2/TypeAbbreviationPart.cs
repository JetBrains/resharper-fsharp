using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class TypeAbbreviationPart : FSharpClassLikePart<IFSharpTypeAbbreviationDeclaration>, Class.IClassPart
  {
    public TypeAbbreviationPart(IFSharpTypeAbbreviationDeclaration declaration)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifiers), declaration.TypeParameters.Count)
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
      MemberDecoration.FromModifiers(Psi.Modifiers.INTERNAL); // should not be accessible from other languages

    protected override byte SerializationTag => (byte) FSharpSerializationTag.TypeAbbreviationPart;
  }
}