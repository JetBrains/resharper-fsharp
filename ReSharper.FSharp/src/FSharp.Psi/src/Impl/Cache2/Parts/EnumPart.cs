using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class EnumPart : FSharpTypeParametersOwnerPart<IFSharpTypeOrExtensionDeclaration>, Enum.IEnumPart
  {
    public EnumPart([NotNull] IFSharpTypeOrExtensionDeclaration declaration, [NotNull] ICacheBuilder builder)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifier, declaration.AllAttributes),
        declaration.TypeParameters, builder)
    {
    }

    public EnumPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpEnum(this);

    public IType GetUnderlyingType()
    {
      // todo: replace with actual type, F# compiler takes type from first valid case
      return TypeFactory.CreateUnknownType(GetPsiModule());
    }

    public IList<IField> Fields =>
      ProcessSubDeclaration<IField, IEnumMemberDeclaration>(input =>
        GetDeclaration() is IFSharpTypeDeclaration decl && decl.TypeRepresentation is IEnumRepresentation repr
          ? repr.EnumMembers
          : EmptyList<IEnumMemberDeclaration>.InstanceList);

    protected override byte SerializationTag => (byte) FSharpPartKind.Enum;
  }
}
