using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing.Lexing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public class FSharpPreprocessedLexer : ILexer<int>
  {
    private struct LexerState
    {
      public readonly TokenNodeType TokenType;
      public readonly int TokenStart;
      public readonly int TokenEnd;
      public readonly int CachingLexerState;

      public LexerState(TokenNodeType tokenType, int tokenStart, int tokenEnd, int cachingLexerState)
      {
        TokenType = tokenType;
        TokenStart = tokenStart;
        TokenEnd = tokenEnd;
        CachingLexerState = cachingLexerState;
      }
    }

    private TokenNodeType myCurrTokenType;
    private int myTokenStart;
    private int myTokenEnd;
    private readonly ILexer<int> myLexer;
    private readonly FSharpPreprocessor myPreprocessor;
    private readonly HashSet<string> myDefinedConstants;
    private readonly PreprocessorState myState = new PreprocessorState();

    public FSharpPreprocessedLexer(ILexer lexer, FSharpPreprocessor preprocessor, HashSet<string> definedConstants)
    {
      myLexer = lexer is CachingLexer cachingLexer ? cachingLexer : new FSharpLexer(lexer.Buffer).ToCachingLexer();
      myPreprocessor = preprocessor;
      myDefinedConstants = definedConstants;
    }

    public FSharpPreprocessedLexer(IBuffer buffer, FSharpPreprocessor preprocessor, HashSet<string> definedConstants)
    {
      myLexer = new FSharpLexer(buffer).ToCachingLexer();
      myPreprocessor = preprocessor;
      myDefinedConstants = definedConstants;
    }

    public void Start()
    {
      myLexer.Start();
      myCurrTokenType = null;
    }

    private TokenNodeType Restore(TokenNodeType token)
    {
      if (token != FSharpTokenType.DEAD_CODE)
      {
        myTokenStart = myLexer.TokenStart;
        myTokenEnd = myLexer.TokenEnd;
      }
      return token;
    }
    
    private TokenNodeType Restore(LexerState stackElem)
    {
      myLexer.CurrentPosition = stackElem.CachingLexerState;
      myTokenStart = stackElem.TokenStart;
      myTokenEnd = stackElem.TokenEnd;
      return stackElem.TokenType;
    }

    private LexerState DeadCodeToken(int dumpStart) =>
      new LexerState(FSharpTokenType.DEAD_CODE, dumpStart, myLexer.TokenStart, myLexer.CurrentPosition);

    private TokenNodeType PreprocessInactiveBranch()
    {
      TokenNodeType tokenType;
      using (LexerStateCookie.Create(myLexer))
      {
        while (myLexer.TokenType == FSharpTokenType.WHITESPACE)
          myLexer.Advance();

        tokenType = myLexer.TokenType;
      }

      if (tokenType == FSharpTokenType.PP_IF_SECTION)
      {
        myState.InIfBlock(false, false);
        return PreprocessInactiveLine();
      }

      if (tokenType == FSharpTokenType.PP_ENDIF)
      {
        myState.FromIfBlock();
        return myState.Condition ? PreprocessLine() : PreprocessInactiveLine();
      }

      if (tokenType == FSharpTokenType.PP_ELSE_SECTION)
      {
        myState.SwitchBranch();
        return myState.Condition ? PreprocessLine() : PreprocessInactiveLine();
      }

      return PreprocessInactiveLine();
    }

    private TokenNodeType PreprocessIfSection()
    {
      using (LexerStateCookie.Create(myLexer))
        myState.InIfBlock(myPreprocessor.Preprocess(myLexer, myDefinedConstants), true);
      return PreprocessLine();
    }

    private TokenNodeType PreprocessLine()
    {
      TokenNodeType tokenType;
      do
      {
        tokenType = myLexer.TokenType;
        myState.EnqueueLexerState(new LexerState(tokenType, myLexer.TokenStart, myLexer.TokenEnd, myLexer.CurrentPosition));
        myLexer.Advance();
      } while (tokenType != FSharpTokenType.NEW_LINE && tokenType != null);
      return TokenType;
    }

    private TokenNodeType PreprocessInactiveLine()
    {
      var dumpStartToken = myLexer.TokenStart;
      TokenNodeType tokenType;
      do
      {
        tokenType = myLexer.TokenType;
        if (tokenType == FSharpTokenType.NEW_LINE)
        {
          var state = DeadCodeToken(dumpStartToken);
          myState.EnqueueLexerState(state);
          myState.EnqueueLexerState(new LexerState(tokenType, myLexer.TokenStart, myLexer.TokenEnd, myLexer.CurrentPosition));
        }
        myLexer.Advance();
      } while (tokenType != FSharpTokenType.NEW_LINE && tokenType != null);
      return TokenType;
    }

    private TokenNodeType PreprocessActiveBranch()
    {
      var tokenType = myLexer.TokenType;
      if (tokenType == FSharpTokenType.PP_IF_SECTION)
        return PreprocessIfSection();

      if (tokenType == FSharpTokenType.PP_ENDIF)
      {
        myState.FromIfBlock();
        return PreprocessLine();
      }

      if (tokenType == FSharpTokenType.PP_ELSE_SECTION)
      {
        myState.SwitchBranch();
        return PreprocessLine();
      }

      return tokenType;
    }

    public void Advance()
    {
      myLexer.Advance();
      LocateToken();
      myCurrTokenType = null;
    }

    private TokenNodeType LocateTokenImpl()
    {
      if (!myState.LexerStates().IsEmpty())
      {
        return Restore(myState.DequeueLexerState());
      }

      return Restore(myLexer.TokenType != null
        ? myState.Condition ? PreprocessActiveBranch() : PreprocessInactiveBranch()
        : null);
    }
  
    object ILexer.CurrentPosition
    {
      get => CurrentPosition;
      set => CurrentPosition = (int) value;
    }
  
    public int CurrentPosition
    {
      get => myLexer.CurrentPosition;
      set => myLexer.CurrentPosition = value;
    }


    public TokenNodeType TokenType
    {
      get
      {
        LocateToken();
        return myCurrTokenType;
      }
    }

    public int TokenStart
    {
      get
      {
        LocateToken();
        return myTokenStart;
      }
    }

    public int TokenEnd
    {
      get
      {
        LocateToken();
        return myTokenEnd;
      }
    }

    public IBuffer Buffer => myLexer.Buffer;

    private void LocateToken()
    {
      if (myCurrTokenType == null)
        myCurrTokenType = LocateTokenImpl();
    }
    
    private class PreprocessorState
    {    
      private enum PreprocessorElseState
      {
        BeforeElse,
        AfterElse,
        InactiveBeforeElse,
        InactiveAfterElse,
        Error
      }

      private struct PreprocessorBlockState
      {
        private PreprocessorElseState myElseState;
        private readonly bool myCondition;

        public PreprocessorBlockState(bool condition, bool hasActiveBranch)
        {
          myCondition = condition;
          myElseState = hasActiveBranch ? PreprocessorElseState.BeforeElse : PreprocessorElseState.InactiveBeforeElse;
        }

        public bool Condition
        {
          get
          {
            switch (myElseState)
            {
              case PreprocessorElseState.BeforeElse: return myCondition;
              case PreprocessorElseState.AfterElse: return !myCondition;
              case PreprocessorElseState.Error: return true;
              default: return false;
            }
          }
        }

        public PreprocessorElseState SwitchBranch()
        {
          switch (myElseState)
          {
            case PreprocessorElseState.BeforeElse:
              myElseState = PreprocessorElseState.AfterElse;
              break;
            case PreprocessorElseState.InactiveBeforeElse:
              myElseState = PreprocessorElseState.InactiveAfterElse;
              break;
            default:
              myElseState = PreprocessorElseState.Error;
              break;
          }
          return myElseState;
        }
      }

      private readonly Stack<PreprocessorBlockState> myStack = new Stack<PreprocessorBlockState>();
      private readonly Queue<LexerState> myQueue = new Queue<LexerState>();

      public IEnumerable<LexerState> LexerStates() => myQueue;

      public void EnqueueLexerState(LexerState state) =>
        myQueue.Enqueue(state);

      public LexerState DequeueLexerState() =>
        myQueue.Dequeue();

      public void InIfBlock(bool condition, bool hasActiveBranch) =>
        myStack.Push(new PreprocessorBlockState(condition, hasActiveBranch));

      public void FromIfBlock()
      {
        if (!myStack.IsEmpty())
          myStack.Pop();
      }

      public void SwitchBranch()
      {
        if (myStack.IsEmpty())
          return;
        var state = myStack.Pop();
        if (state.SwitchBranch() == PreprocessorElseState.Error)
          myStack.Clear();
        else
          myStack.Push(state);
      }

      public bool Condition => myStack.IsEmpty() || myStack.Peek().Condition;
    }
  }
}
