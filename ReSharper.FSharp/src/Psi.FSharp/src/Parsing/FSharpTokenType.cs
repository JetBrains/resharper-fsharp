using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Psi.FSharp.Parsing
{
  public partial class FSharpTokenType
  {
    public static readonly NodeTypeSet RightBraces;
    public static readonly NodeTypeSet LeftBraces;
    public static readonly NodeTypeSet CommentsOrWhitespaces;
    public static readonly NodeTypeSet Keywords;

    static FSharpTokenType()
    {
      CommentsOrWhitespaces = new NodeTypeSet(COMMENT, WHITESPACE, NEW_LINE);
      RightBraces = new NodeTypeSet(RPAREN, RBRACK, RBRACE);
      LeftBraces = new NodeTypeSet(LPAREN, LBRACK, LBRACE);

      Keywords = new NodeTypeSet(
        PUBLIC,
        PRIVATE,
        INTERNAL,
        NAMESPACE,
        MODULE,
        KEYWORD);
    }

    private sealed class WhitespaceNodeType : FSharpTokenType
    {
      public WhitespaceNodeType(int nodeTypeIndex) : base("WHITE_SPACE", nodeTypeIndex)
      {
      }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      {
        return new Whitespace(buffer, startOffset, endOffset);
      }
    }

    private sealed class NewLineNodeType : FSharpTokenType
    {
      public NewLineNodeType(int nodeTypeIndex) : base("NEW_LINE", nodeTypeIndex)
      {
      }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      {
        return new NewLine(buffer, startOffset, endOffset);
      }
    }

    public const int WHITESPACE_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 1;
    public static readonly TokenNodeType WHITESPACE = new WhitespaceNodeType(WHITESPACE_NODE_TYPE_INDEX);

    public const int NEW_LINE_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 2;
    public static readonly TokenNodeType NEW_LINE = new NewLineNodeType(NEW_LINE_NODE_TYPE_INDEX);
  }
}