using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing.Lexing;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public class FSharpPreprocessedLexer : ILexer<FSharpLexerState>
  {
    private TokenNodeType currTokenType;
    private readonly FSharpLexer myLexer;
    private readonly FSharpPreprocessor myPreprocessor;
    private readonly HashSet<string> myDefinedConstants;
    private readonly PreprocessorState myState = new PreprocessorState();

    public FSharpPreprocessedLexer(IBuffer buffer, FSharpPreprocessor preprocessor, HashSet<string> definedConstants)
    {
      myLexer = new FSharpLexer(buffer);
      myPreprocessor = preprocessor;
      myDefinedConstants = definedConstants;
    }

    public FSharpPreprocessedLexer(
      IBuffer buffer,
      int startOffset,
      int endOffset,
      FSharpPreprocessor preprocessor,
      HashSet<string> definedConstants)
    {
      myLexer = new FSharpLexer(buffer, startOffset, endOffset);
      myPreprocessor = preprocessor;
      myDefinedConstants = definedConstants;
    }

    public void Start()
    {
      myLexer.Start();
      currTokenType = null;
    }

    private void Restore(FSharpLexerState stackElem)
    {
      myLexer.CurrentPosition = stackElem;
    }

    private FSharpLexerState DeadCodeToken(FSharpLexerState state, int dumpStart)
    {
      state.currTokenType = FSharpTokenType.DEAD_CODE;
      state.yy_buffer_start = dumpStart;
      state.yy_buffer_end = myLexer.TokenStart;
      state.yy_buffer_index = myLexer.TokenStart;
      return state;
    }

    private TokenNodeType PreprocessInactiveBranch()
    {
      TokenNodeType tokenType;
      using (LexerStateCookie.Create(myLexer))
      {
        while (myLexer.TokenType == FSharpTokenType.WHITESPACE)
        {
          myLexer.Advance();
        }

        tokenType = myLexer.TokenType;
      }
      
      switch (tokenType)
      {
        case var token when token == FSharpTokenType.PP_IF_SECTION:
          myState.InIfBlock(false, false);
          return PreprocessInactiveLine();
        case var token when token == FSharpTokenType.PP_ENDIF:
          myState.FromIfBlock();
          return myState.Condition ? PreprocessLine() : PreprocessInactiveLine();
        case var token when token == FSharpTokenType.PP_ELSE_SECTION:
          myState.SwitchBranch();
          return myState.Condition ? PreprocessLine() : PreprocessInactiveLine();
        default:
          return PreprocessInactiveLine();
      }
    }

    private TokenNodeType PreprocessIfSection()
    {
      using (LexerStateCookie.Create(myLexer))
      {
        myState.InIfBlock(myPreprocessor.Preprocess(myLexer, myDefinedConstants), true);
      }
      return PreprocessLine();
    }

    private TokenNodeType PreprocessLine()
    {
      TokenNodeType tokenType;
      do
      {
        tokenType = myLexer.TokenType;
        myState.EnqueueLexerState(myLexer.CurrentPosition);
        myLexer.Advance();
      } while (!(tokenType == FSharpTokenType.NEW_LINE || tokenType == null));
      return TokenType;
    }

    private TokenNodeType PreprocessInactiveLine()
    {
      var dumpStartToken = myLexer.TokenStart;
      TokenNodeType tokenType;
      do
      {
        tokenType = myLexer.TokenType;
        switch (tokenType)
        {
          case var token when token == FSharpTokenType.NEW_LINE:
            var state = DeadCodeToken(myLexer.CurrentPosition, dumpStartToken);
            myState.EnqueueLexerState(state);
            myState.EnqueueLexerState(myLexer.CurrentPosition);
            break;
        }
        myLexer.Advance();
      } while (!(tokenType == FSharpTokenType.NEW_LINE || tokenType == null));
      return TokenType;
    }

    private TokenNodeType PreprocessActiveBranch()
    {
      switch (myLexer.TokenType)
      {
        case var token when token == FSharpTokenType.PP_IF_SECTION:
          return PreprocessIfSection();
        case var token when token == FSharpTokenType.PP_ENDIF:
          myState.FromIfBlock();
          return PreprocessLine();
        case var token when token == FSharpTokenType.PP_ELSE_SECTION:
          myState.SwitchBranch();
          return PreprocessLine();
        default:
          return myLexer.TokenType;
      }
    }

    public void Advance()
    {
      myLexer.Advance();
      LocateToken();
      currTokenType = null;
    }

    public TokenNodeType _locateToken()
    {
      if (!myState.LexerStates().IsEmpty())
      {
        Restore(myState.DequeueLexerState());
        return myLexer.TokenType;
      }

      return myLexer.TokenType != null
        ? myState.Condition ? PreprocessActiveBranch() : PreprocessInactiveBranch()
        : null;
    }
  
    object ILexer.CurrentPosition
    {
      get => CurrentPosition;
      set => CurrentPosition = (FSharpLexerState) value;
    }
  
    public FSharpLexerState CurrentPosition
    {
      get => myLexer.CurrentPosition;
      set => myLexer.CurrentPosition = value;
    }


    public TokenNodeType TokenType
    {
      get
      {
        LocateToken();
        return currTokenType;
      }
    }

    public int TokenStart
    {
      get
      {
        LocateToken();
        return myLexer.TokenStart;
      }
    }

    public int TokenEnd
    {
      get
      {
        LocateToken();
        return myLexer.TokenEnd;
      }
    }

    public IBuffer Buffer => myLexer.Buffer;

    private void LocateToken()
    {
      if (currTokenType == null)
      {
        currTokenType = _locateToken();
      }
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
      private readonly Queue<FSharpLexerState> myQueue = new Queue<FSharpLexerState>();

      public IEnumerable<FSharpLexerState> LexerStates()
      {
        return myQueue;
      }
      public void EnqueueLexerState(FSharpLexerState state)
      {
        myQueue.Enqueue(state);
      }

      public FSharpLexerState DequeueLexerState()
      {
        return myQueue.Dequeue();
      }

      public void InIfBlock(bool condition, bool hasActiveBranch)
      {
        myStack.Push(new PreprocessorBlockState(condition, hasActiveBranch));
      }

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
        var elseState = state.SwitchBranch();
        if (elseState == PreprocessorElseState.Error)
          myStack.Clear();
        else
          myStack.Push(state);
      }

      public bool Condition => myStack.IsEmpty() || myStack.Peek().Condition;
    }
  }
}
