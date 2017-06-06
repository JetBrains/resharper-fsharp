using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Parts
{
  internal class EnumPart : FSharpTypeParametersOwnerPart<IEnumDeclaration>, Enum.IEnumPart
  {
    public EnumPart(IEnumDeclaration declaration, ICacheBuilder builder)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifiers, declaration.AttributesEnumerable),
        declaration.TypeParameters, builder)
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
      ProcessSubDeclaration<IField, IEnumMemberDeclaration>(input => input.EnumMembers);

    protected override byte SerializationTag => (byte) FSharpPartKind.Enum;
  }
}