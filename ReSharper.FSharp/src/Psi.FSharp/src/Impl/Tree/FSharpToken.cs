using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public class FSharpToken : BindedToBufferLeafElement, IFSharpTreeNode, ITokenNode
  {
    public FSharpToken(NodeType nodeType, [NotNull] IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      : base(nodeType, buffer, startOffset, endOffset)
    {
    }

    public override PsiLanguageType Language => FSharpLanguage.Instance;
    public TokenNodeType GetTokenType() => (TokenNodeType) NodeType;
    public override string ToString() => base.ToString() + "(type:" + NodeType + ", text:" + GetText() + ")";
    public virtual void Accept(TreeNodeVisitor visitor) => visitor.VisitNode(this);

    public virtual void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context) =>
      visitor.VisitNode(this, context);

    public virtual TResult Accept<TContext, TResult>(TreeNodeVisitor<TContext, TResult> visitor, TContext context) =>
      visitor.VisitNode(this, context);
  }
}