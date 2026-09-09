using System;
using System.Collections.Generic;
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
    private readonly PreprocessorState myState = new();

    public FSharpPreprocessedLexer(ILexer lexer, FSharpPreprocessor preprocessor, HashSet<string> definedConstants)
    {
      myLexer = lexer as CachingLexer ?? lexer.ToCachingLexer();
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
      new(FSharpTokenType.DEAD_CODE, dumpStart, myLexer.TokenStart, myLexer.CurrentPosition);
  
    private bool EvaluateBranchCondition()
    {
      using (LexerStateCookie.Create(myLexer))
        return myPreprocessor.Preprocess(myLexer, myDefinedConstants);
    }

    private bool TryApplyDirective(TokenNodeType tokenType, out bool lineIsActive)
    {
      if (tokenType == FSharpTokenType.PP_IF_SECTION)
        lineIsActive = myState.StartIfSection(EvaluateBranchCondition);

      else if (tokenType == FSharpTokenType.PP_ELIF_SECTION)
        lineIsActive = myState.ElifBranch(EvaluateBranchCondition);

      else if (tokenType == FSharpTokenType.PP_ELSE_SECTION)
        lineIsActive = myState.ElseBranch();

      else if (tokenType == FSharpTokenType.PP_ENDIF)
        lineIsActive = myState.EndIfSection();

      else
      {
        lineIsActive = false;
        return false;
      }

      return true;
    }

    private TokenNodeType PreprocessActiveBranch()
    {
      var tokenType = myLexer.TokenType;

      if (!TryApplyDirective(tokenType, out var lineIsActive))
        return tokenType;

      return lineIsActive 
        ? PreprocessLine() 
        : PreprocessInactiveLine();
    }

    private TokenNodeType PreprocessInactiveBranch()
    {
      TokenNodeType tokenType;

      using (LexerStateCookie.Create(myLexer))
      {
        while (myLexer.TokenType == FSharpTokenType.WHITESPACE)
          myLexer.Advance();

        tokenType = myLexer.TokenType;
      }

      return TryApplyDirective(tokenType, out var lineIsActive) && lineIsActive
        ? PreprocessLine()
        : PreprocessInactiveLine();
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

    private void LocateToken() =>
      myCurrTokenType ??= LocateTokenImpl();

    private class PreprocessorState
    {
      private struct PreprocessorBlockState
      {
        private bool myAnyBranchTaken;
        private bool myAfterElse;

        public PreprocessorBlockState(Func<bool> condition, bool outerCondition)
        {
          OuterCondition = outerCondition;
          Condition = outerCondition && condition();
          myAnyBranchTaken = Condition;
          myAfterElse = false;
        }

        public bool Condition { get; private set; }

        public bool OuterCondition { get; }

        public bool SwitchBranch(Func<bool> condition, bool isElse)
        {
          if (myAfterElse)
            return false;

          Condition = OuterCondition && !myAnyBranchTaken && condition();
          myAnyBranchTaken |= Condition;
          myAfterElse = isElse;
          return true;
        }
      }

      private readonly Stack<PreprocessorBlockState> myStack = new();
      private readonly Queue<LexerState> myQueue = new();

      public IEnumerable<LexerState> LexerStates() => myQueue;

      public void EnqueueLexerState(LexerState state) =>
        myQueue.Enqueue(state);

      public LexerState DequeueLexerState() =>
        myQueue.Dequeue();

      public bool StartIfSection(Func<bool> condition)
      {
        var outerCondition = Condition;
        myStack.Push(new PreprocessorBlockState(condition, outerCondition));
        return outerCondition;
      }

      public bool EndIfSection()
      {
        if (!myStack.IsEmpty())
          myStack.Pop();
        return Condition;
      }

      public bool ElifBranch(Func<bool> condition) => SwitchBranch(condition, isElse: false);
      public bool ElseBranch() => SwitchBranch(static () => true, isElse: true);

      private bool SwitchBranch(Func<bool> condition, bool isElse)
      {
        if (myStack.IsEmpty())
          return true;

        var state = myStack.Pop();
        if (!state.SwitchBranch(condition, isElse))
        {
          myStack.Clear();
          return true;
        }
        myStack.Push(state);

        return state.OuterCondition;
      }

      public bool Condition => myStack.IsEmpty() || myStack.Peek().Condition;
    }
  }
}
