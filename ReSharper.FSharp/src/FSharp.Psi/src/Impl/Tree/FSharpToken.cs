using System.Text;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public class FSharpToken : FSharpTokenBase
  {
    public FSharpToken(NodeType nodeType, string text)
    {
      NodeType = nodeType;
      myText = text;
    }
    
    public override NodeType NodeType { get; }
    private readonly string myText;
    
    public override string GetText() => myText;
    public override int GetTextLength() => myText.Length;
  }
  
  public abstract class FSharpTokenBase : LeafElementBase, IFSharpTreeNode, ITokenNode
  {
    
    public override PsiLanguageType Language => FSharpLanguage.Instance;
    public TokenNodeType GetTokenType() => (TokenNodeType) NodeType;

    public override string ToString() => base.ToString() + "(type:" + NodeType + ", text:" + GetText() + ")";

    public override StringBuilder GetText(StringBuilder to)
    {
      to.Append(GetText());
      return to;
    }

    public override IBuffer GetTextAsBuffer() => new StringBuffer(GetText());

    public virtual void Accept(TreeNodeVisitor visitor) =>
      visitor.VisitNode(this);

    public virtual void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context) =>
      visitor.VisitNode(this, context);

    public virtual TResult Accept<TContext, TResult>(TreeNodeVisitor<TContext, TResult> visitor, TContext context) =>
      visitor.VisitNode(this, context);
  }
}