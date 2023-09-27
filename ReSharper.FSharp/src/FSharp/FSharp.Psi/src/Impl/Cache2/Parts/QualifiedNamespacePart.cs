using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  public class QualifiedNamespacePart : NamespacePart
  {
    public QualifiedNamespacePart(TreeOffset offset, string shortName) : base(null, offset, shortName)
    {
    }

    public QualifiedNamespacePart(IReader reader) : base(reader)
    {
    }

    protected override ICachedDeclaration2 FindDeclaration(IFile file, ICachedDeclaration2 candidateDeclaration)
    {
      return null;
    }

    public override IDeclaration GetDeclaration()
    {
      return null;
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.QualifiedNamespace;

    public override string ToString() => $"{GetType().Name}:{ShortName}";
  }
}
