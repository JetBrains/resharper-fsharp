using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class EnumPart : FSharpTypePart<IFSharpEnumDeclaration>, Enum.IEnumPart
  {
    public EnumPart(IFSharpEnumDeclaration declaration, bool isHidden) : base(declaration,
      ModifiersUtil.GetDecoration(declaration.AccessModifiers, declaration.AttributesEnumerable), isHidden,
      declaration.TypeParameters.Count)
    {
    }

    public EnumPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new Enum(this);
    }

    public IType GetUnderlyingType()
    {
      // todo: replace with actual type, F# compiler takes type from first valid case
      return TypeFactory.CreateUnknownType(GetPsiModule());
    }

    public IList<IField> Fields =>
      ProcessSubDeclaration<IField, IFSharpEnumMemberDeclaration>(input => input.EnumMembers);

    protected override byte SerializationTag => (byte) FSharpPartKind.Enum;
  }
}