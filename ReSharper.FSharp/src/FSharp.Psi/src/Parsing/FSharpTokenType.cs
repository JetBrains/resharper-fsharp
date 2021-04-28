using System;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
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

      public FSharpTokenNodeType(string name, int index, string representation) : base(name, index)
      {
        FSharpNodeTypeIndexer.Instance.Add(this, index);
        TokenRepresentation = representation;
      }

      public override LeafElementBase Create(string text)
      {
        if (CreateIdentifierTokenTypes[this])
          return new FSharpIdentifierToken(this, text);

        if (IsComment)
          return new FSharpComment(this, text);

        if (Strings[this])
          return new FSharpString(this, text);

        return this != DEAD_CODE
          ? new FSharpToken(this, text)
          : new FSharpDeadCodeToken(this, text);
      }

      public override bool IsWhitespace => this == WHITESPACE || this == NEW_LINE;
      public override bool IsComment => this == LINE_COMMENT || this == BLOCK_COMMENT;
      public override bool IsStringLiteral => StringsLiterals[this];
      public override bool IsConstantLiteral => Literals[this];
      public override bool IsIdentifier => Identifiers[this];
      public override bool IsKeyword => Keywords[this];

      public override string TokenRepresentation { get; }

      public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset) =>
        throw new NotImplementedException();
    }

    private abstract class FixedTokenNodeElement : FSharpTokenBase
    {
    }

    private abstract class FixedIdentifierTokenNodeElement : FSharpTokenBase, IFSharpIdentifierToken
    {
      public string Name => GetText();
      public ITokenNode IdentifierToken => this;
      public TreeTextRange NameRange => this.GetTreeTextRange();
    }

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

      public override bool IsFiltered => true;
      public override LeafElementBase Create(string text) => new Whitespace(text);
    }

    private sealed class NewLineNodeType : FSharpTokenNodeType
    {
      public NewLineNodeType(int nodeTypeIndex) : base("NEW_LINE", nodeTypeIndex)
      {
      }

      public override bool IsFiltered => true;
      public override LeafElementBase Create(string text) => new NewLine(text);
    }

    private sealed class LineCommentNodeType : FSharpTokenNodeType
    {
      public LineCommentNodeType(int nodeTypeIndex) : base("LINE_COMMENT", nodeTypeIndex)
      {
      }

      public override bool IsFiltered => true;

      public override LeafElementBase Create(string text) =>
        text.Length > 2 && text[2] == '/' && (text.Length == 3 || text[3] != '/')
          ? new DocComment(this, text)
          : new FSharpComment(this, text);
    }

    private sealed class BlockCommentNodeType : FSharpTokenNodeType
    {
      public BlockCommentNodeType(int nodeTypeIndex) : base("BLOCK_COMMENT", nodeTypeIndex)
      {
      }

      public override bool IsFiltered => true;
      public override LeafElementBase Create(string text) => new FSharpComment(this, text);
    }

    public const int WHITESPACE_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 1;
    public static readonly TokenNodeType WHITESPACE = new WhitespaceNodeType(WHITESPACE_NODE_TYPE_INDEX);

    public const int NEW_LINE_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 2;
    public static readonly TokenNodeType NEW_LINE = new NewLineNodeType(NEW_LINE_NODE_TYPE_INDEX);

    public const int LINE_COMMENT_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 3;
    public static readonly TokenNodeType LINE_COMMENT = new LineCommentNodeType(LINE_COMMENT_NODE_TYPE_INDEX);

    public const int BLOCK_COMMENT_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 4;
    public static readonly TokenNodeType BLOCK_COMMENT = new BlockCommentNodeType(BLOCK_COMMENT_NODE_TYPE_INDEX);

    public const int CHAMELEON_NODE_TYPE_INDEX = LAST_GENERATED_TOKEN_TYPE_INDEX + 5;
    public static readonly TokenNodeType CHAMELEON = new FSharpTokenNodeType("CHAMELEON", CHAMELEON_NODE_TYPE_INDEX);

    public static readonly NodeTypeSet RightBraces;
    public static readonly NodeTypeSet LeftBraces;
    public static readonly NodeTypeSet AccessModifiersKeywords;
    public static readonly NodeTypeSet Keywords;
    public static readonly NodeTypeSet Identifiers;
    public static readonly NodeTypeSet StringsLiterals;
    public static readonly NodeTypeSet InterpolatedStrings;
    public static readonly NodeTypeSet Strings;
    public static readonly NodeTypeSet Literals;
    public static readonly NodeTypeSet CreateIdentifierTokenTypes;

    static FSharpTokenType()
    {
      AccessModifiersKeywords = new NodeTypeSet(PUBLIC, PRIVATE, INTERNAL);

      LeftBraces = new NodeTypeSet(
        LPAREN,
        LBRACE,
        LBRACK,
        LQUOTE_UNTYPED,
        LBRACK_BAR,
        LBRACK_LESS,
        LQUOTE_TYPED,
        LBRACE_BAR);

      RightBraces = new NodeTypeSet(
        RPAREN,
        RBRACE,
        RBRACK,
        RQUOTE_UNTYPED,
        BAR_RBRACK,
        RQUOTE_TYPED,
        GREATER_RBRACK,
        BAR_RBRACE);

      Keywords = new NodeTypeSet(
        ABSTRACT,
        AND,
        AS,
        ASSERT,
        BASE,
        BEGIN,
        CLASS,
        CONST,
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
        RARROW);

      Identifiers = new NodeTypeSet(
        IDENTIFIER,
        SYMBOLIC_OP,
        AMP_AMP,
        GREATER,
        PLUS,
        MINUS,
        LESS,
        LPAREN_STAR_RPAREN);

      StringsLiterals = new NodeTypeSet(
        CHARACTER_LITERAL,
        STRING,
        VERBATIM_STRING,
        TRIPLE_QUOTED_STRING,
        BYTEARRAY,
        VERBATIM_BYTEARRAY);

      InterpolatedStrings = new NodeTypeSet(
        REGULAR_INTERPOLATED_STRING,
        REGULAR_INTERPOLATED_STRING_START,
        REGULAR_INTERPOLATED_STRING_MIDDLE,
        REGULAR_INTERPOLATED_STRING_END,
        VERBATIM_INTERPOLATED_STRING,
        VERBATIM_INTERPOLATED_STRING_START,
        VERBATIM_INTERPOLATED_STRING_MIDDLE,
        VERBATIM_INTERPOLATED_STRING_END,
        TRIPLE_QUOTE_INTERPOLATED_STRING,
        TRIPLE_QUOTE_INTERPOLATED_STRING_START,
        TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE,
        TRIPLE_QUOTE_INTERPOLATED_STRING_END,
        UNFINISHED_REGULAR_INTERPOLATED_STRING,
        UNFINISHED_VERBATIM_INTERPOLATED_STRING,
        UNFINISHED_TRIPLE_QUOTE_INTERPOLATED_STRING);

      Strings = StringsLiterals.Union(InterpolatedStrings);

      Literals = new NodeTypeSet(
        IEEE32,
        IEEE64,
        DECIMAL,
        BYTE,
        INT16,
        INT32,
        INT64,
        SBYTE,
        UINT16,
        UINT32,
        UINT64,
        BIGNUM,
        NATIVEINT,
        UNATIVEINT);

      CreateIdentifierTokenTypes = new NodeTypeSet(
        AMP_AMP,
        COLON_COLON,
        GLOBAL,
        GREATER,
        IDENTIFIER,
        EQUALS,
        LESS,
        LPAREN_STAR_RPAREN,
        MINUS,
        PERCENT,
        PERCENT_PERCENT,
        PLUS,
        QMARK,
        QMARK_QMARK,
        STAR,
        SYMBOLIC_OP);
    }
  }
}
