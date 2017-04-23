using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class UnionPart : FSharpClassLikePart<IFSharpTypeParametersOwnerDeclaration>, Class.IClassPart
  {
    private static readonly string[] ourExtendsListShortNames =
      {"IStructuralEquatable", "IStructuralComparable", "IComparable"};

    public UnionPart(IFSharpTypeParametersOwnerDeclaration declaration, bool isHidden) :
      base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifiers), isHidden,
        declaration.TypeParameters.Count)
    {
    }

    public UnionPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpUnion(this);
    }

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.NONE;
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.Union;

    public override string[] ExtendsListShortNames => ourExtendsListShortNames;
  }
}