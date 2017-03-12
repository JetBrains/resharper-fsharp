using System.Collections.Generic;
using ICSharpCode.NRefactory;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class EnumPart : FSharpTypePart<IFSharpEnumDeclaration>, Enum.IEnumPart
  {
    public EnumPart(IFSharpEnumDeclaration declaration) : base(declaration,
      ModifiersUtil.GetDecoration(declaration.AccessModifiers), declaration.TypeParameters.Count)
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
      return GetPsiModule().GetPredefinedType().Int;
    }

    public IList<IField> Fields =>
//      ProcessSubDeclaration<IField, IFSharpEnumMemberDeclaration>(input => input.EnumMemberDeclarations);
      EmptyList<IField>.Instance;

    protected override byte SerializationTag => (byte) FSharpSerializationTag.EnumPart;
  }
}