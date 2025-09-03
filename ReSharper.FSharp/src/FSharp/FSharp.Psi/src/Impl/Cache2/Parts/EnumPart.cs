using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class EnumPart : FSharpTypeParametersOwnerPart<IFSharpTypeOrExtensionDeclaration>, IFSharpEnumPart
  {
    public EnumPart([NotNull] IFSharpTypeOrExtensionDeclaration declaration, [NotNull] ICacheBuilder builder)
      : base(declaration, FSharpModifiersUtil.GetDecoration(declaration.AccessModifier, declaration.Attributes),
        declaration.TypeParameterDeclarations, builder)
    {
    }

    public EnumPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement(IPsiModule module) =>
      new FSharpEnum(this);

    public IType GetUnderlyingType()
    {
      if (GetDeclaration() is IFSharpTypeDeclaration { TypeRepresentation: IEnumRepresentation repr })
        foreach (var memberDeclaration in repr.EnumCases)
          if (memberDeclaration.Expression is ILiteralExpr expr)
            return expr.Type();

      return TypeFactory.CreateUnknownType(GetPsiModule());
    }

    public IEnumerable<IField> Fields
    {
      get
      {
        if (GetDeclaration() is IFSharpTypeDeclaration { TypeRepresentation: IEnumRepresentation repr })
          foreach (var memberDeclaration in repr.EnumCases)
            if (memberDeclaration.DeclaredElement is { } field)
              yield return (IField) @field;
      }
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.Enum;
  }
}
