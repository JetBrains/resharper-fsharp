using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Psi.FSharp.Parsing
{
  public partial class FSharpTokenType
  {
    public class FSharpTokenNodeType : TokenNodeType
    {
      public FSharpTokenNodeType(string name, int index) : base(name, index)
      {
        FSharpNodeTypeIndexer.Instance.Add(this, index);
        TokenRepresentation = name;
      }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      {
        return this == IDENTIFIER || this == OPERATOR
          ? new FSharpIdentifierToken(this, buffer, startOffset, endOffset)
          : new FSharpToken(this, buffer, startOffset, endOffset);
      }

      public override bool IsWhitespace => this == WHITESPACE || this == NEW_LINE;
      public override bool IsComment => this == COMMENT;
      public override bool IsStringLiteral => this == STRING;
      public override bool IsConstantLiteral => this == LITERAL;
      public override bool IsIdentifier => this == IDENTIFIER;
      public override bool IsKeyword => Keywords[this];

      public override string TokenRepresentation { get; }
    }

    public static readonly NodeTypeSet RightBraces;
    public static readonly NodeTypeSet LeftBraces;
    public static readonly NodeTypeSet CommentsOrWhitespaces;
    public static readonly NodeTypeSet AccessModifiersKeywords;
    public static readonly NodeTypeSet Keywords;

    static FSharpTokenType()
    {
      CommentsOrWhitespaces = new NodeTypeSet(COMMENT, WHITESPACE, NEW_LINE);
      RightBraces = new NodeTypeSet(RPAREN, RBRACK, RBRACE);
      LeftBraces = new NodeTypeSet(LPAREN, LBRACK, LBRACE);
      AccessModifiersKeywords = new NodeTypeSet(PUBLIC, PRIVATE, INTERNAL);

      Keywords = new NodeTypeSet(
        // todo: add other keywords
        PUBLIC,
        PRIVATE,
        INTERNAL,
        NAMESPACE,
        MODULE,
        KEYWORD);
    }

    private sealed class WhitespaceNodeType : FSharpTokenNodeType
    {
      public WhitespaceNodeType(int nodeTypeIndex) : base("WHITE_SPACE", nodeTypeIndex)
      {
      }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      {
        return new Whitespace(buffer, startOffset, endOffset);
      }
    }

    private sealed class NewLineNodeType : FSharpTokenNodeType
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