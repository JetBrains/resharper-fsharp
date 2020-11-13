using System;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Diagnostics;
using JetBrains.Util;

// ReSharper disable RedundantDisableWarningComment
// ReSharper disable InconsistentNaming

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing.Lexing
{
  public struct FSharpLexerState
  {
    public TokenNodeType currTokenType;
    public int yy_buffer_index;
    public int yy_buffer_start;
    public int yy_buffer_end;
    public int yy_lexical_state;
  }

  partial class FSharpLexerGenerated : ILexer<FSharpLexerState>
  {
    private TokenNodeType currTokenType;
    private readonly ReusableBufferRange myBuffer = new ReusableBufferRange();
    protected static readonly LexerDictionary<TokenNodeType> keywords = new LexerDictionary<TokenNodeType>();

    private int zzNestedCommentLevel;
    private int zzParenLevel;
    private int zzTokenLength;
    private int zzBrackLevel;

    static FSharpLexerGenerated()
    {
      foreach (var nodeType in FSharpTokenType.Keywords)
      {
        var keyword = (TokenNodeType) nodeType;
        keywords[keyword.TokenRepresentation] = keyword;
      }
    }

    private void clear()
    {
      int yy_state = yy_state_dtrans[yy_lexical_state];
      yy_buffer_start = yy_buffer_index;
      if (YY_NOT_ACCEPT != yy_acpt[yy_state])
      {
        yy_buffer_end = yy_buffer_index;
      }
    }

    private TokenNodeType FindKeywordByCurrentToken()
    {
      return keywords.GetValueSafe(myBuffer, yy_buffer, yy_buffer_start, yy_buffer_end);
    }

    private TokenNodeType makeToken(TokenNodeType type)
    {
      return currTokenType = type;
    }

    private void initTokenLength()
    {
      zzTokenLength = 0;
    }

    private void increaseTokenLength(int n)
    {
      zzTokenLength += n;
    }

    private TokenNodeType setTokenLength(TokenNodeType type)
    {
      yy_buffer_start = yy_buffer_end - (yylength() + zzTokenLength);
      return makeToken(type);
    }

    private void yypushback(int n)
    {
      yy_buffer_index -= n;
      yy_buffer_end -= n;
    }

    private int zzLevel => zzParenLevel + zzBrackLevel;

    private void initBlockComment()
    {
      if (yy_lexical_state == LINE)
      {
        yybegin(IN_BLOCK_COMMENT_FROM_LINE);
      }
      else
      {
        yybegin(IN_BLOCK_COMMENT);
      }
      zzNestedCommentLevel++;
    }

    private TokenNodeType initIdent()
    {
      TokenNodeType keyword = FindKeywordByCurrentToken();
      // use if you need to separate the reserved keyword
      // TokenNodeType reservedKeyword = FindReservedKeywordByCurrentToken();
      return makeToken(keyword != null ? keyword : FSharpTokenType.IDENTIFIER);
    }

    private TokenNodeType identInTypeApp()
    {
      TokenNodeType keyword = FindKeywordByCurrentToken();
      if (keyword != null) {
          return makeToken(keyword);
      }
      return makeToken(FSharpTokenType.IDENTIFIER);
    }

    private TokenNodeType identInInitTypeApp()
    {
      TokenNodeType keyword = FindKeywordByCurrentToken();
      if (keyword != null) {
          yybegin(LINE);
          return makeToken(keyword);
      }
      return makeToken(FSharpTokenType.IDENTIFIER);
    }
  
    private TokenNodeType fillBlockComment(TokenNodeType tokenType)
    {
      if (yy_lexical_state == IN_BLOCK_COMMENT_FROM_LINE)
      {
        yybegin(LINE);
      }
      else
      {
        riseFromParenLevel(0);
      }
      zzNestedCommentLevel = 0;
      return setTokenLength(tokenType);
    }

    private void checkGreatRBrack(int state, int finalState)
    {
      if (zzBrackLevel > 0)
      {
        zzBrackLevel--;
        yybegin(SYMBOLIC_OPERATOR);
      }
      else
      {
        initSmashAdjacent(state, finalState);
      }
    }
    private void initSmashAdjacent(int state, int finalState)
    {
      zzParenLevel--;
      if (zzLevel <= 0)
      {
        yybegin(finalState);
      }
      else
      {
        yybegin(state);
      }
    }

    private void deepInto()
    {
      if (zzLevel > 1 && yy_lexical_state == INIT_ADJACENT_TYAPP)
        yybegin(ADJACENT_TYAPP);
    }

    private void deepIntoParenLevel()
    {
      zzParenLevel++;
      deepInto();
    }

    private void deepIntoBrackLevel()
    {
      zzBrackLevel++;
      deepInto();
    }

    private void riseFromParenLevel(int n)
    {
      zzParenLevel -= n;
      if (zzLevel > 1)
      {
        yybegin(ADJACENT_TYAPP);
      }
      else if (zzLevel <= 0)
      {
        yybegin(LINE);
      }
      else
      {
        yybegin(INIT_ADJACENT_TYAPP);
      }
    }

    private void initAdjacentTypeApp()
    {
      if (yytext()[yylength() - 1] == '/')
      {
        yypushback(2);
      }
      else
      {
        yypushback(1);
      }
      zzParenLevel = 0;
      zzBrackLevel = 0;
      yybegin(INIT_ADJACENT_TYAPP);
    }

    private void adjacentTypeCloseOp()
    {
      zzParenLevel -= yylength();
      yypushback(yylength());
      if (zzLevel > 0)
      {
        yybegin(SYMBOLIC_OPERATOR);
      }
      else
      {
        yybegin(GREATER_OP_SYMBOLIC_OP);
      }
    }

    private void exitSmash(int state)
    {
      if (yy_lexical_state == state)
      {
        yybegin(LINE);
      }
      else
      {
        riseFromParenLevel(0);
      }
    }

    private void initSmash(int initState, int anotherState)
    {
      if (yy_lexical_state == LINE)
      {
        yybegin(initState);
      }
      else
      {
        yybegin(anotherState);
      }
    }

    private void exitGreaterOp()
    {
      if (yy_lexical_state == GREATER_OP)
      {
        riseFromParenLevel(0);
      }
      else
      {
        yybegin(SYMBOLIC_OPERATOR);
      }
    }

    public void Start()
    {
      Start(0, yy_buffer.Length, YYINITIAL);
    }

    public void Start(int startOffset, int endOffset, uint state)
    {
      yy_buffer_index = startOffset;
      yy_buffer_start = startOffset;
      yy_buffer_end = startOffset;
      yy_eof_pos = endOffset;
    
      var unpack = FSharpLexerStatePacker.Unpack(state);
      yy_lexical_state = unpack.First;
      zzParenLevel = unpack.Second;
      
      currTokenType = null;
    }

    public void Advance()
    {
      locateToken();
      currTokenType = null;
    }

    public FSharpLexerState CurrentPosition
    {
      get
      {
        FSharpLexerState tokenPosition;
        tokenPosition.currTokenType = currTokenType;
        tokenPosition.yy_buffer_index = yy_buffer_index;
        tokenPosition.yy_buffer_start = yy_buffer_start;
        tokenPosition.yy_buffer_end = yy_buffer_end;
        tokenPosition.yy_lexical_state = yy_lexical_state;
        return tokenPosition;
      }
      set
      {
        currTokenType = value.currTokenType;
        yy_buffer_index = value.yy_buffer_index;
        yy_buffer_start = value.yy_buffer_start;
        yy_buffer_end = value.yy_buffer_end;
        yy_lexical_state = value.yy_lexical_state;
      }
    }

    object ILexer.CurrentPosition
    {
      get { return CurrentPosition; }
      set { CurrentPosition = (FSharpLexerState) value; }
    }

    public TokenNodeType TokenType
    {
      get
      {
        locateToken();
        return currTokenType;
      }
    }

    public int TokenStart
    {
      get
      {
        locateToken();
        return yy_buffer_start;
      }
    }

    public int TokenEnd
    {
      get
      {
        locateToken();
        return yy_buffer_end;
      }
    }

    public int LexemIndent { get { return 7; } }
    public IBuffer Buffer { get { return yy_buffer; } }

    protected int BufferIndex { get { return yy_buffer_index; } set { yy_buffer_index = value; } }
    protected int BufferStart { get { return yy_buffer_start; } set { yy_buffer_start = value; } }
    protected int BufferEnd { set { yy_buffer_end = value; } }
    public    int EOFPos { get { return yy_eof_pos; } }
    protected int LexicalState { get { return yy_lexical_state; } }

    private void locateToken()
    {
      if (currTokenType == null)
      {
        currTokenType = _locateToken();
      }
    }

    public uint LexerStateEx
    {
      get
      {
        return FSharpLexerStatePacker.Pack(yy_lexical_state, zzParenLevel);
      }
    }
  }
  
  /// <summary>
  /// State contract:
  ///  5 bits  - yy_lexical_state (should be le 31)
  ///  27 bits - paren depth in type app state
  ///  In invalid state all bits are 1
  /// </summary>
  public static class FSharpLexerStatePacker
  {
    public static Pair<int, int> Unpack(uint state)
    {
      if (state == LexerStateConstants.InvalidState)
        throw new ArgumentException("Invalid lexer state");

      const uint mask5bit = 0b11111;
      const uint mask27bit = 0x7FFFFFF;

      // Restore yy state from first 5 bits
      var yy_lexical_state = (int)(state & mask5bit);

      // Restore items count from next 27 bits
      var zzParenLevel = (int)(state >> 5 & mask27bit);

      return Pair.Of(yy_lexical_state, zzParenLevel);
    }

    public static uint Pack(int yy_lexical_state, int zzParenLevel)
    {
      // We can store only 31 yy states
      Assertion.Assert(yy_lexical_state <= 31, "yy_lexical_state overflow");
      if (yy_lexical_state > 31)
        return LexerStateConstants.InvalidState;

      // Store yy state into first 5 bits
      uint state = (uint)yy_lexical_state;
      // Store items count in next 27 bits
      state ^= (uint)zzParenLevel << 5;

      return state;
    }
  }
}
