using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class WhitespaceBase : FSharpToken, IWhitespaceNode
  {
    protected WhitespaceBase(NodeType nodeType, [NotNull] IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      : base(nodeType, buffer, startOffset, endOffset)
    {
    }

    public override bool IsFiltered() => true;
    public override string ToString() => base.ToString() + " spaces:" + "\"" + GetText() + "\"";
    public abstract bool IsNewLine { get; }
  }
}