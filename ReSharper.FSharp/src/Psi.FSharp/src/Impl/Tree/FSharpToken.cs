using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  public class FSharpToken : BindedToBufferLeafElement, IFSharpTreeNode, ITokenNode
  {
    public FSharpToken(NodeType nodeType, [NotNull] IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      : base(nodeType, buffer, startOffset, endOffset)
    {
    }

    public override PsiLanguageType Language => FSharpLanguage.Instance;

    public TokenNodeType GetTokenType()
    {
      return (TokenNodeType) NodeType;
    }

    public override string ToString()
    {
      return base.ToString() + "(type:" + NodeType + ", text:" + GetText() + ")";
    }

    public virtual void Accept(TreeNodeVisitor visitor)
    {
      visitor.VisitNode(this);
    }

    public virtual void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context)
    {
      visitor.VisitNode(this, context);
    }

    public virtual TResult Accept<TContext, TResult>(TreeNodeVisitor<TContext, TResult> visitor, TContext context)
    {
      return visitor.VisitNode(this, context);
    }
  }
}