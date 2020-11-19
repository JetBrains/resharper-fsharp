using System;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Diagnostics;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing.Lexing
{
  public struct FSharpLexerState
  {
    public TokenNodeType CurrentTokenType;
    public int BufferIndex;
    public int BufferStart;
    public int BufferEnd;
    public int LexicalState;
    public ImmutableStack<FSharpLexerInterpolatedStringState> InterpolatedStringPreviousStates;
  }

  public struct FSharpLexerContext
  {
    public int LexerState;
    public int ParenLevel;
    public int BrackLevel;
    public int NestedCommentLevel;
  }

  public enum FSharpInterpolatedStringKind
  {
    Regular,
    Verbatim,
    TripleQuote
  }

  public enum InterpolatedStringStackItem
  {
    Paren,
    Brace,
    Bracket
  }
  
  public struct FSharpLexerInterpolatedStringState
  {
    public FSharpInterpolatedStringKind Kind;
    public FSharpLexerContext PreviousLexerContext;
    public ImmutableStack<InterpolatedStringStackItem> InterpolatedStringStack;
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

    private ImmutableStack<FSharpLexerInterpolatedStringState> myInterpolatedStringStates =
      ImmutableStack<FSharpLexerInterpolatedStringState>.Empty;

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

    private void yypushback(int n)
    {
      yy_buffer_index -= n;
      yy_buffer_end -= n;
    }

    private int Level => myParenLevel + myBrackLevel;

    private void StartInterpolatedString(FSharpInterpolatedStringKind kind)
    {
      var previousContext = new FSharpLexerContext
      {
        LexerState = yy_lexical_state,
        ParenLevel = myParenLevel,
        BrackLevel = myBrackLevel,
        NestedCommentLevel = myNestedCommentLevel
      };

      var interpolatedStringState = new FSharpLexerInterpolatedStringState
      {
        Kind = kind,
        PreviousLexerContext = previousContext,
        InterpolatedStringStack = ImmutableStack<InterpolatedStringStackItem>.Empty
      };
      myInterpolatedStringStates = myInterpolatedStringStates.Push(interpolatedStringState);
    }

    private void FinishInterpolatedString()
    {
      Assertion.Assert(!myInterpolatedStringStates.IsEmpty, "!myInterpolatedStringPreviousStates.IsEmpty");

      var interpolatedStringState = myInterpolatedStringStates.Peek();
      var prevContext = interpolatedStringState.PreviousLexerContext;

      yy_lexical_state = prevContext.LexerState;
      myBrackLevel = prevContext.BrackLevel;
      myParenLevel = prevContext.ParenLevel;
      myNestedCommentLevel = prevContext.NestedCommentLevel;

      myInterpolatedStringStates = myInterpolatedStringStates.Pop();
    }

    private void PushInterpolatedStringItem(InterpolatedStringStackItem item)
    {
      if (!myInterpolatedStringStates.IsEmpty && myInterpolatedStringStates.Peek() is var state)
      {
        myInterpolatedStringStates = myInterpolatedStringStates.Pop();

        state.InterpolatedStringStack = state.InterpolatedStringStack.Push(item);
        myInterpolatedStringStates = myInterpolatedStringStates.Push(state);
      }
    }

    private bool PopInterpolatedStringItem(InterpolatedStringStackItem item)
    {
      if (myInterpolatedStringStates.IsEmpty || !(myInterpolatedStringStates.Peek() is var state)) return false;

      if (state.InterpolatedStringStack.IsEmpty && item == InterpolatedStringStackItem.Brace)
      {
        yypushback(1);
        yybegin(ToState(myInterpolatedStringStates.Peek()));
        Clear();

        return true;
      }

      if (state.InterpolatedStringStack.Peek() == item)
      {
        state.InterpolatedStringStack = state.InterpolatedStringStack.Pop();
        myInterpolatedStringStates = myInterpolatedStringStates.Pop();
        myInterpolatedStringStates = myInterpolatedStringStates.Push(state);
      }

      return false;
    }

    public static int ToState(FSharpLexerInterpolatedStringState interpolatedStringState) =>
      interpolatedStringState.Kind switch
      {
        FSharpInterpolatedStringKind.Regular => ISR,
        FSharpInterpolatedStringKind.Verbatim => ISV,
        FSharpInterpolatedStringKind.TripleQuote => ISTQ,
        _ => LINE // todo: check this
      };

    private void InitBlockComment()
    {
      yybegin(yy_lexical_state == LINE ? IN_BLOCK_COMMENT_FROM_LINE : IN_BLOCK_COMMENT);
      myNestedCommentLevel++;
    }

    private void InitStringInClockComment()
    {
      Assertion.Assert(yy_lexical_state == IN_BLOCK_COMMENT || yy_lexical_state == IN_BLOCK_COMMENT_FROM_LINE,
        "yy_lexical_state == IN_BLOCK_COMMENT || yy_lexical_state == IN_BLOCK_COMMENT_FROM_LINE");

      yybegin(yy_lexical_state == IN_BLOCK_COMMENT ? STRING_IN_COMMENT : STRING_IN_COMMENT_FROM_LINE);
    }

    private void FinishStringInClockComment()
    {
      Assertion.Assert(yy_lexical_state == STRING_IN_COMMENT || yy_lexical_state == STRING_IN_COMMENT_FROM_LINE,
        "yy_lexical_state == STRING_IN_COMMENT || yy_lexical_state == STRING_IN_COMMENT_FROM_LINE");

      yybegin(yy_lexical_state == STRING_IN_COMMENT ? IN_BLOCK_COMMENT : IN_BLOCK_COMMENT_FROM_LINE);
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

    private TokenNodeType FillBlockComment()
    {
      if (yy_lexical_state == IN_BLOCK_COMMENT_FROM_LINE)
        yybegin(LINE);
      else
        RiseFromParenLevel(0);
      myNestedCommentLevel = 0;
      return SetTokenLength(FSharpTokenType.BLOCK_COMMENT);
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
      if (Level > 1 && yy_lexical_state == INIT_TYPE_APP)
        yybegin(TYPE_APP);
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
        yybegin(TYPE_APP);
      else if (Level <= 0)
        yybegin(LINE);
      else
        yybegin(INIT_TYPE_APP);
    }

    private void InitTypeApp()
    {
      yypushback(yytext()[yylength() - 1] == '/' ? 2 : 1);
      myParenLevel = 0;
      myBrackLevel = 0;
      yybegin(INIT_TYPE_APP);
    }

    private void AdjacentTypeCloseOp()
    {
      myParenLevel -= yylength();
      yypushback(yylength());
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

      var (lexicalState, parenLevel) = FSharpLexerStateEncoding.DecodeLexerState(state);
      yy_lexical_state = lexicalState;
      myParenLevel = parenLevel;

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
        tokenPosition.InterpolatedStringPreviousStates = myInterpolatedStringStates;
        return tokenPosition;
      }
      set
      {
        myCurrentTokenType = value.CurrentTokenType;
        yy_buffer_index = value.BufferIndex;
        yy_buffer_start = value.BufferStart;
        yy_buffer_end = value.BufferEnd;
        yy_lexical_state = value.LexicalState;
        myInterpolatedStringStates = value.InterpolatedStringPreviousStates;
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
      !myInterpolatedStringStates.IsEmpty || myNestedCommentLevel > 0 || myBrackLevel > 0
        ? LexerStateConstants.InvalidState
        : FSharpLexerStateEncoding.EncodeLexerState(yy_lexical_state, myParenLevel);
  }

  /// <summary>
  /// State contract:
  ///  5 bits  - yy_lexical_state (should be le 31)
  ///  27 bits - paren depth in type app state
  ///  In invalid state all bits are 1
  /// </summary>
  public static class FSharpLexerStateEncoding
  {
    private const uint LexicalStateMask = 0b11111;
    private const uint TypeAppStateMask = 0x7FFFFFF;
    private const int TypeAppStateOffset = 5;

    public static Pair<int, int> DecodeLexerState(uint state)
    {
      if (state == LexerStateConstants.InvalidState)
        throw new ArgumentException("Invalid lexer state");

      // todo: check nesting is not overflowed
      var lexicalState = (int) (state & LexicalStateMask);
      var parenLevel = (int) (state >> TypeAppStateOffset & TypeAppStateMask);

      return Pair.Of(lexicalState, parenLevel);
    }

    public static uint EncodeLexerState(int lexicalState, int parenLevel)
    {
      Assertion.Assert(lexicalState <= LexicalStateMask, "lexicalState overflow");

      var state = (uint) lexicalState;
      state ^= (uint) parenLevel << TypeAppStateOffset;

      return state;
    }
  }
}
