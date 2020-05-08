using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class AbbreviatedTypeOrUnionCaseDeclaration
  {
    private ITypeReferenceName NamedTypeReferenceName =>
      AbbreviatedType is INamedTypeUsage namedTypeUsage ? namedTypeUsage.ReferenceName : null;

    public override IFSharpIdentifierLikeNode NameIdentifier =>
      NamedTypeReferenceName is var referenceName && TypeReferenceCanBeUnionCaseDeclaration(referenceName)
        ? referenceName.Identifier
        : null;

    protected override string DeclaredElementName => NameIdentifier.GetSourceName();

    protected override IDeclaredElement CreateDeclaredElement() =>
      AbbreviatedType is INamedTypeUsage namedTypeUsage && namedTypeUsage.ReferenceName is var referenceName &&
      referenceName.Reference.Resolve().DeclaredElement == null
        ? new FSharpUnionCaseProperty(this)
        : null;

    public bool CanBeUnionCase =>
      TypeReferenceCanBeUnionCaseDeclaration(NamedTypeReferenceName);

    private static bool TypeReferenceCanBeUnionCaseDeclaration(IReferenceName referenceName) =>
      referenceName != null && referenceName.Qualifier == null && referenceName.TypeArgumentList == null;
  }
}
