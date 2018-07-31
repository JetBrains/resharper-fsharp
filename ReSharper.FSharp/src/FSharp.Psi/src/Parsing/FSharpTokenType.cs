using System;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
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

      public override LeafElementBase Create(string text)
      {
        if (Identifiers[this])
          return new FSharpIdentifierToken(this, text);

        if (IsComment)
          return new FSharpComment(this, text);

        if (IsStringLiteral)
          return new FSharpString(this, text);

        return this != DEAD_CODE
          ? new FSharpToken(this, text)
          : new FSharpDeadCodeToken(this, text);
      }

      public override bool IsWhitespace => this == WHITESPACE || this == NEW_LINE;
      public override bool IsComment => this == LINE_COMMENT || this == COMMENT;
      public override bool IsStringLiteral => Strings[this];
      public override bool IsConstantLiteral => this == LITERAL;
      public override bool IsIdentifier => Identifiers[this];
      public override bool IsKeyword => Keywords[this];

      public override string TokenRepresentation { get; }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset) =>
        throw new NotImplementedException();
    }

    private abstract class FixedTokenNodeElement : FSharpTokenBase { }
    
    private class FixedTokenNodeType : FSharpTokenNodeType, IFixedTokenNodeType
    {
      protected FixedTokenNodeType(string name, int index, string representation) : base(name, index) =>
        TokenRepresentation = representation;

      public override string TokenRepresentation { get; }

      public override LeafElementBase Create(string token) =>
        Create(null, TreeOffset.Zero, TreeOffset.Zero);

      public LeafElementBase Create() =>
        throw new NotImplementedException();
    }
    
    private sealed class WhitespaceNodeType : FSharpTokenNodeType
    {
      public WhitespaceNodeType(int nodeTypeIndex) : base("WHITE_SPACE", nodeTypeIndex)
      {
      }

      public override LeafElementBase Create(string text) => new Whitespace(text);
    }

    private sealed class NewLineNodeType : FSharpTokenNodeType
    {
      public NewLineNodeType(int nodeTypeIndex) : base("NEW_LINE", nodeTypeIndex)
      {
      }

      public override LeafElementBase Create(string text) => new NewLine(text);
    }

    public const int WHITESPACE_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 1;
    public static readonly TokenNodeType WHITESPACE = new WhitespaceNodeType(WHITESPACE_NODE_TYPE_INDEX);

    public const int NEW_LINE_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 2;
    public static readonly TokenNodeType NEW_LINE = new NewLineNodeType(NEW_LINE_NODE_TYPE_INDEX);


    public static readonly NodeTypeSet RightBraces;
    public static readonly NodeTypeSet LeftBraces;
    public static readonly NodeTypeSet CommentsOrWhitespaces;
    public static readonly NodeTypeSet AccessModifiersKeywords;
    public static readonly NodeTypeSet Keywords;
    public static readonly NodeTypeSet Identifiers;
    public static readonly NodeTypeSet Strings;

    static FSharpTokenType()
    {
      CommentsOrWhitespaces = new NodeTypeSet(COMMENT, WHITESPACE, NEW_LINE);
      AccessModifiersKeywords = new NodeTypeSet(PUBLIC, PRIVATE, INTERNAL);

      LeftBraces = new NodeTypeSet(
        LPAREN,
        LBRACE,
        LBRACK,
        LQUOTE_UNTYPED,
        LBRACK_BAR,
        LBRACK_LESS,
        LQUOTE_TYPED,
        LESS);

      RightBraces = new NodeTypeSet(
        RPAREN,
        RBRACE,
        RBRACK,
        RQUOTE_UNTYPED,
        BAR_RBRACK,
        RQUOTE_TYPED,
        GREATER_RBRACK,
        GREATER);

      Keywords = new NodeTypeSet(
        ABSTRACT,
        AND,
        AS,
        ASSERT,
        BASE,
        BEGIN,
        CLASS,
        DEFAULT,
        DELEGATE,
        DO,
        DO_BANG,
        DONE,
        DOWNCAST,
        DOWNTO,
        ELIF,
        ELSE,
        END,
        EXCEPTION,
        EXTERN,
        FALSE,
        FINALLY,
        FIXED,
        FOR,
        FUN,
        FUNCTION,
        GLOBAL,
        IF,
        IN,
        INHERIT,
        INLINE,
        INTERFACE,
        INTERNAL,
        LAZY,
        LET,
        MATCH,
        MATCH_BANG,
        MEMBER,
        MODULE,
        MUTABLE,
        NAMESPACE,
        NEW,
        NULL,
        OF,
        OPEN,
        OR,
        OVERRIDE,
        PRIVATE,
        PUBLIC,
        REC,
        RETURN,
        STATIC,
        STRUCT,
        THEN,
        TO,
        TRUE,
        TRY,
        TYPE,
        UPCAST,
        USE,
        VAL,
        VOID,
        WHEN,
        WHILE,
        WITH,
        YIELD,

        HASH,
        RARROW,
        OTHER_KEYWORD);

      Identifiers = new NodeTypeSet(
        IDENTIFIER,
        OPERATOR,
        GREATER,
        LESS);

      Strings = new NodeTypeSet(
        CHAR,
        STRING,
        VERBATIM_STRING,
        TRIPLE_QUOTE_STRING,
        BYTEARRAY);
    }
  }
}