using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeUsageOrUnionCaseDeclaration
  {
    private ITypeReferenceName NamedTypeReferenceName =>
      TypeUsage is INamedTypeUsage namedTypeUsage ? namedTypeUsage.ReferenceName : null;

    public override IFSharpIdentifierLikeNode NameIdentifier =>
      NamedTypeReferenceName is { } referenceName && TypeReferenceCanBeUnionCaseDeclaration(referenceName)
        ? referenceName.Identifier
        : null;

    protected override string DeclaredElementName => NameIdentifier.GetSourceName();

    protected override IDeclaredElement CreateDeclaredElement() =>
      TypeUsage is INamedTypeUsage { ReferenceName: { } referenceName } &&
      referenceName.Reference.Resolve().DeclaredElement == null
        ? new FSharpUnionCaseProperty(this)
        : null;

    public bool CanBeUnionCase =>
      TypeReferenceCanBeUnionCaseDeclaration(NamedTypeReferenceName);

    private static bool TypeReferenceCanBeUnionCaseDeclaration(IReferenceName referenceName) =>
      referenceName is { Qualifier: null } && referenceName.TypeArgumentList == null;

    public bool HasFields => false;
    public FSharpUnionCaseClass NestedType => null;
  }
}
