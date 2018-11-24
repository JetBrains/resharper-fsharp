using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public class FSharpIdentifierToken : FSharpToken
  {
    public FSharpIdentifierToken(NodeType nodeType, [NotNull] IBuffer buffer, TreeOffset startOffset,
      TreeOffset endOffset) : base(nodeType, buffer, startOffset, endOffset)
    {
    }

    public override ReferenceCollection GetFirstClassReferences() =>
      new ReferenceCollection(new FSharpSymbolReference(this));
  }
}