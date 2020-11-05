using System;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Diagnostics;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing.Lexing
{
  public struct FSharpLexerState
  {
    public TokenNodeType CurrentTokenType;
    public int BufferIndex;
    public int BufferStart;
    public int BufferEnd;
    public int LexicalState;
  }

  partial class FSharpLexerGenerated : ILexer<FSharpLexerState>
  {
    private TokenNodeType myCurrentTokenType;
    private readonly ReusableBufferRange myBuffer = new ReusableBufferRange();
    protected static readonly LexerDictionary<TokenNodeType> Keywords = new LexerDictionary<TokenNodeType>();

    private int myNestedCommentLevel;
    private int myParenLevel;
    private int myTokenLength;
    private int myBrackLevel;

    static FSharpLexerGenerated()
    {
      foreach (var nodeType in FSharpTokenType.Keywords)
      {
        var keyword = (TokenNodeType) nodeType;
        Keywords[keyword.TokenRepresentation] = keyword;
      }
    }

    private void Clear()
    {
      var yyState = yy_state_dtrans[yy_lexical_state];
      yy_buffer_start = yy_buffer_index;
      if (YY_NOT_ACCEPT != yy_acpt[yyState]) 
        yy_buffer_end = yy_buffer_index;
    }

    private TokenNodeType FindKeywordByCurrentToken() =>
      Keywords.GetValueSafe(myBuffer, yy_buffer, yy_buffer_start, yy_buffer_end);

    private TokenNodeType MakeToken(TokenNodeType type) =>
      myCurrentTokenType = type;

    private void InitTokenLength() =>
      myTokenLength = 0;

    private void IncreaseTokenLength(int n) =>
      myTokenLength += n;

    private TokenNodeType SetTokenLength(TokenNodeType type)
    {
      yy_buffer_start = yy_buffer_end - (yylength() + myTokenLength);
      return MakeToken(type);
    }

    private void PushBack(int n)
    {
      yy_buffer_index -= n;
      yy_buffer_end -= n;
    }

    private int Level => myParenLevel + myBrackLevel;

    private void InitBlockComment()
    {
      yybegin(yy_lexical_state == LINE ? IN_BLOCK_COMMENT_FROM_LINE : IN_BLOCK_COMMENT);
      myNestedCommentLevel++;
    }

    private TokenNodeType InitIdent()
    {
      var keyword = FindKeywordByCurrentToken();
      // use if you need to separate the reserved keyword
      // TokenNodeType reservedKeyword = FindReservedKeywordByCurrentToken();
      return MakeToken(keyword ?? FSharpTokenType.IDENTIFIER);
    }

    private TokenNodeType IdentInTypeApp()
    {
      var keyword = FindKeywordByCurrentToken();
      return MakeToken(keyword ?? FSharpTokenType.IDENTIFIER);
    }

    private TokenNodeType IdentInInitTypeApp()
    {
      var keyword = FindKeywordByCurrentToken();
      if (keyword != null)
      {
        yybegin(LINE);
        return MakeToken(keyword);
      }

      return MakeToken(FSharpTokenType.IDENTIFIER);
    }

    private TokenNodeType FillBlockComment(TokenNodeType tokenType)
    {
      if (yy_lexical_state == IN_BLOCK_COMMENT_FROM_LINE)
        yybegin(LINE);
      else
        RiseFromParenLevel(0);
      myNestedCommentLevel = 0;
      return SetTokenLength(tokenType);
    }

    private void CheckGreatRBrack(int state, int finalState)
    {
      if (myBrackLevel > 0)
      {
        myBrackLevel--;
        yybegin(SYMBOLIC_OPERATOR);
      }
      else
      {
        InitSmashAdjacent(state, finalState);
      }
    }

    private void InitSmashAdjacent(int state, int finalState)
    {
      myParenLevel--;
      yybegin(Level <= 0 ? finalState : state);
    }

    private void DeepInto()
    {
      if (Level > 1 && yy_lexical_state == INIT_ADJACENT_TYPE_APP)
        yybegin(ADJACENT_TYPE_APP);
    }

    private void DeepIntoParenLevel()
    {
      myParenLevel++;
      DeepInto();
    }

    private void DeepIntoBrackLevel()
    {
      myBrackLevel++;
      DeepInto();
    }

    private void RiseFromParenLevel(int n)
    {
      myParenLevel -= n;
      if (Level > 1)
        yybegin(ADJACENT_TYPE_APP);
      else if (Level <= 0)
        yybegin(LINE);
      else
        yybegin(INIT_ADJACENT_TYPE_APP);
    }

    private void InitAdjacentTypeApp()
    {
      PushBack(yytext()[yylength() - 1] == '/' ? 2 : 1);
      myParenLevel = 0;
      myBrackLevel = 0;
      yybegin(INIT_ADJACENT_TYPE_APP);
    }

    private void AdjacentTypeCloseOp()
    {
      myParenLevel -= yylength();
      PushBack(yylength());
      yybegin(Level > 0 ? SYMBOLIC_OPERATOR : GREATER_OP_SYMBOLIC_OP);
    }

    private void ExitSmash(int state)
    {
      if (yy_lexical_state == state)
        yybegin(LINE);
      else
        RiseFromParenLevel(0);
    }

    private void InitSmash(int initState, int anotherState) =>
      yybegin(yy_lexical_state == LINE ? initState : anotherState);

    private void ExitGreaterOp()
    {
      if (yy_lexical_state == GREATER_OP)
        RiseFromParenLevel(0);
      else
        yybegin(SYMBOLIC_OPERATOR);
    }

    public void Start() =>
      Start(0, yy_buffer.Length, YYINITIAL);

    public void Start(int startOffset, int endOffset, uint state)
    {
      yy_buffer_index = startOffset;
      yy_buffer_start = startOffset;
      yy_buffer_end = startOffset;
      yy_eof_pos = endOffset;

      var unpack = FSharpLexerStatePacker.Unpack(state);
      yy_lexical_state = unpack.First;
      myParenLevel = unpack.Second;

      myCurrentTokenType = null;
    }

    public void Advance()
    {
      LocateToken();
      myCurrentTokenType = null;
    }

    public FSharpLexerState CurrentPosition
    {
      get
      {
        FSharpLexerState tokenPosition;
        tokenPosition.CurrentTokenType = myCurrentTokenType;
        tokenPosition.BufferIndex = yy_buffer_index;
        tokenPosition.BufferStart = yy_buffer_start;
        tokenPosition.BufferEnd = yy_buffer_end;
        tokenPosition.LexicalState = yy_lexical_state;
        return tokenPosition;
      }
      set
      {
        myCurrentTokenType = value.CurrentTokenType;
        yy_buffer_index = value.BufferIndex;
        yy_buffer_start = value.BufferStart;
        yy_buffer_end = value.BufferEnd;
        yy_lexical_state = value.LexicalState;
      }
    }

    object ILexer.CurrentPosition
    {
      get => CurrentPosition;
      set => CurrentPosition = (FSharpLexerState) value;
    }

    public TokenNodeType TokenType
    {
      get
      {
        LocateToken();
        return myCurrentTokenType;
      }
    }

    public int TokenStart
    {
      get
      {
        LocateToken();
        return yy_buffer_start;
      }
    }

    public int TokenEnd
    {
      get
      {
        LocateToken();
        return yy_buffer_end;
      }
    }

    public int LexemIndent => 7;
    public IBuffer Buffer => yy_buffer;
    public int EOFPos => yy_eof_pos;

    private void LocateToken() =>
      myCurrentTokenType ??= _locateToken();

    public uint LexerStateEx =>
      FSharpLexerStatePacker.Pack(yy_lexical_state, myParenLevel);
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

      const uint mask5Bit = 0b11111;
      const uint mask27Bit = 0x7FFFFFF;

      // Restore yy state from first 5 bits
      var lexicalState = (int) (state & mask5Bit);

      // Restore items count from next 27 bits
      var parenLevel = (int) (state >> 5 & mask27Bit);

      return Pair.Of(lexicalState, parenLevel);
    }

    public static uint Pack(int lexicalState, int parenLevel)
    {
      // We can store only 31 states
      Assertion.Assert(lexicalState <= 31, "lexicalState overflow");

      // Store state into first 5 bits
      var state = (uint) lexicalState;
      // Store items count in next 27 bits
      state ^= (uint) parenLevel << 5;

      return state;
    }
  }
}
