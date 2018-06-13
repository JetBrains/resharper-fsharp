using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public class FSharpLexer : ILexer
  {
    // todo: add this tag to FCS
    private const int InactiveCodeTag = 7;

    private readonly IDocument myDocument;
    private readonly Int32<DocLine> myDocumentLineCount;
    private readonly FSharpSourceTokenizer mySourceTokenizer;

    private FSharpLineTokenizer myLineTokenizer;
    private Int32<DocLine> myLineIndex = (Int32<DocLine>) (-1);
    private int myLineStartOffset;
    private long myState;

    private Tuple<FSharpOption<FSharpToken>, long> myNextTokenAndState =
      new Tuple<FSharpOption<FSharpToken>, long>(null, (long) FSharpTokenizerColorState.InitialState);

    public FSharpLexer([NotNull] IDocument document, FSharpList<string> defines)
    {
      myDocument = document;
      myDocumentLineCount = document.GetLineCount();
      mySourceTokenizer = new FSharpSourceTokenizer(defines, null);
      Buffer = document.Buffer;
    }

    public void Start()
    {
      if (StartNewLine())
        Advance();
    }

    public void Advance()
    {
      if (myNextTokenAndState.Item1 == null) // check if no token found, end of line
      {
        if (!StartNewLine())
        {
          // did not start a new line, end of file
          TokenType = null;
          return;
        }

        TokenType = FSharpTokenType.NEW_LINE;
        TokenStart = TokenEnd;
        TokenEnd = myLineIndex < myDocumentLineCount
          ? myDocument.GetLineStartOffset(myLineIndex)
          : Buffer.Length;
        return;
      }

      var token = myNextTokenAndState.Item1.Value;
      TokenStart = token.LeftColumn + myLineStartOffset;
      TokenType = GetTokenType(token);
      Seek();

      var initialState = FSharpColorState;
      var isInLineComment = initialState == FSharpTokenizerColorState.SingleLineComment;
      while (ShouldConcatNextToken(initialState))
      {
        while (myNextTokenAndState.Item1 == null)
        {
          if (!ShouldConcatNextToken(initialState))
            break;
          
          if (isInLineComment)
            break;

          if (!StartNewLine())
          {
            TokenEnd = Buffer.Length;
            return;
          }
        }

        if (myNextTokenAndState.Item1 != null)
          token = myNextTokenAndState.Item1.Value;
        Seek();
      }

      // sometimes tokenizer may skip idents after #, look for next token start column // todo
      TokenEnd = myLineStartOffset + (myNextTokenAndState.Item1?.Value.LeftColumn ?? token.RightColumn + 1);
    }

    private TokenNodeType GetTokenType(FSharpToken token)
    {
      // todo: next or current state?
      var state = FSharpLineTokenizer.ColorStateOfLexState(myNextTokenAndState.Item2);
      if (state == FSharpTokenizerColorState.VerbatimString)
        return FSharpTokenType.VERBATIM_STRING;
      if (state == FSharpTokenizerColorState.TripleQuoteString)
        return FSharpTokenType.TRIPLE_QUOTE_STRING;

      return token.GetTokenType();
    }
    
    private bool ShouldConcatNextToken(FSharpTokenizerColorState initial)
    {
      var current = FSharpColorState;
      if (current == FSharpTokenizerColorState.Token || current == FSharpTokenizerColorState.EndLineThenToken ||
          current == FSharpTokenizerColorState.EndLineThenSkip)
        return false;

      if (initial == FSharpTokenizerColorState.String || initial == FSharpTokenizerColorState.TripleQuoteString ||
          initial == FSharpTokenizerColorState.VerbatimString)
        return current == initial;

      if (initial == FSharpTokenizerColorState.IfDefSkip && current == FSharpTokenizerColorState.IfDefSkip)
        return myNextTokenAndState.Item1?.Value.Token.Tag == InactiveCodeTag;

      return current != FSharpTokenizerColorState.String && current != FSharpTokenizerColorState.TripleQuoteString &&
             current != FSharpTokenizerColorState.VerbatimString && current != FSharpTokenizerColorState.IfDefSkip;
    }

    private bool StartNewLine()
    {
      myLineIndex++;
      if (myLineIndex >= myDocumentLineCount)
        return false;

      myLineTokenizer = mySourceTokenizer.CreateLineTokenizer(myDocument.GetLineText(myLineIndex));
      myLineStartOffset = myDocument.GetLineStartOffset(myLineIndex);
      Seek();

      return true;
    }

    private void Seek()
    {
      myState = myNextTokenAndState.Item2;
      myNextTokenAndState = myLineTokenizer.ScanToken(myNextTokenAndState.Item2);
    }

    internal FSharpTokenizerColorState FSharpColorState =>
      FSharpLineTokenizer.ColorStateOfLexState(myState);

    public object CurrentPosition { get; set; }
    public TokenNodeType TokenType { get; private set; }
    public int TokenStart { get; private set; }
    public int TokenEnd { get; private set; }
    public IBuffer Buffer { get; }
  }
}