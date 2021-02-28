using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class EnumPart : FSharpTypeParametersOwnerPart<IFSharpTypeOrExtensionDeclaration>, Enum.IEnumPart
  {
    public EnumPart([NotNull] IFSharpTypeOrExtensionDeclaration declaration, [NotNull] ICacheBuilder builder)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifier, declaration.Attributes),
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

    public IEnumerable<IField> Fields
    {
      get
      {
        if (GetDeclaration() is IFSharpTypeDeclaration decl && decl.TypeRepresentation is IEnumRepresentation repr)
          foreach (var memberDeclaration in repr.EnumMembers)
            if (memberDeclaration.DeclaredElement is { } field)
              yield return (IField) field;
      }
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.Enum;
  }
}
