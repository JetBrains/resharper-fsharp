using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public class FSharpDeadCodeToken : FSharpToken
  {
    public FSharpDeadCodeToken(NodeType nodeType, [NotNull] IBuffer buffer, TreeOffset startOffset,
      TreeOffset endOffset) : base(nodeType, buffer, startOffset, endOffset)
    {
    }
  }
}