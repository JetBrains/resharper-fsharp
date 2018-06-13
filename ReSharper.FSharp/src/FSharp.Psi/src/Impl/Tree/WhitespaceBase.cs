using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class WhitespaceBase : FSharpToken, IWhitespaceNode
  {
    protected WhitespaceBase(NodeType nodeType, string text) : base(nodeType, text)
    {
    }

    public override bool IsFiltered() => true;
    public override string ToString() => base.ToString() + " spaces:" + "\"" + GetText() + "\"";
    public abstract bool IsNewLine { get; }
  }
}