using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Parts
{
  public class DeclaredNamespacePart : NamespacePart
  {
    public DeclaredNamespacePart(IFSharpNamespaceDeclaration declaration)
      : base(declaration, declaration.GetTreeStartOffset(), declaration.ShortName)
    {
    }

    public DeclaredNamespacePart(IReader reader) : base(reader)
    {
    }

    protected override ICachedDeclaration2 FindDeclaration(IFile file, ICachedDeclaration2 candidateDeclaration)
    {
      if (Offset < TreeOffset.Zero) return null;
      if (candidateDeclaration is IFSharpNamespaceDeclaration) return candidateDeclaration;
      return null;
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.DeclaredNamespace;
  }
}