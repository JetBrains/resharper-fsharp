using System;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Psi.FSharp.Parsing
{
  public class FSharpLexer : ILexer
  {
    // todo: add this tag to FCS
    private const int InactiveCodeTag = 7;

    private readonly IDocument myDocument;
    private readonly Int32<DocLine> myDocumentLineCount;
    private readonly FSharpSourceTokenizer mySourceTokenizer;

    private FSharpLineTokenizer myLineTokenizer;
    private Int32<DocLine> myLineIndex = Int32<DocLine>.O;
    private int myLineStartOffset;
    private Tuple<FSharpOption<FSharpTokenInfo>, long> myNextTokenAndState;

    public FSharpLexer([NotNull] IDocument document, FSharpList<string> defines)
    {
      myDocument = document;
      myDocumentLineCount = document.GetLineCount();
      mySourceTokenizer = new FSharpSourceTokenizer(defines, null);
      Buffer = document.Buffer;
    }

    public void Start()
    {
      StartNewLine((long) FSharpTokenizerColorState.InitialState);
      Advance();
    }

    public void Advance()
    {
      if (myNextTokenAndState.Item1 == null) // end of line
      {
        if (myLineIndex == myDocumentLineCount)
        {
          TokenType = null;
          return;
        }

        TokenStart = TokenEnd;
        myLineIndex++;

        TokenEnd = myLineIndex < myDocumentLineCount
          ? myDocument.GetLineStartOffset(myLineIndex)
          : myDocument.Buffer.Length;

        TokenType = FSharpTokenType.NEW_LINE;
        if (myLineIndex < myDocumentLineCount) StartNewLine(myNextTokenAndState.Item2);
        return;
      }
      FindToken();
    }

    private void StartNewLine(long state)
    {
      myLineTokenizer = mySourceTokenizer.CreateLineTokenizer(myDocument.GetLineText(myLineIndex));
      myLineStartOffset = myDocument.GetLineStartOffset(myLineIndex);
      myNextTokenAndState = myLineTokenizer.ScanToken(state);
    }

    private void FindToken()
    {
      var token = myNextTokenAndState.Item1.Value;
      TokenStart = token.LeftColumn + myLineStartOffset;
      myNextTokenAndState = myLineTokenizer.ScanToken(myNextTokenAndState.Item2);
      while (myNextTokenAndState.Item1 != null)
      {
        // some tokens like multi-word strings come as separate tokens, concatenate them
        var nextTokenClass = myNextTokenAndState.Item1.Value.CharClass;
        if (token.CharClass != nextTokenClass || !ShouldConcatenate(token))
          break;
        token = myNextTokenAndState.Item1.Value;
        myNextTokenAndState = myLineTokenizer.ScanToken(myNextTokenAndState.Item2);
      }

      var nextToken = myNextTokenAndState.Item1;
      // sometimes tokenizer may skip idents after #, look for next token start column
      TokenEnd = myLineStartOffset + (nextToken?.Value.LeftColumn ?? token.RightColumn + 1);
      TokenType = FSharpTokenInfoEx.GetTokenType(token);
    }

    private static bool ShouldConcatenate(FSharpTokenInfo token)
    {
      var tokenTag = token.Tag;
      if (tokenTag == InactiveCodeTag) return true;
      if (tokenTag == FSharpTokenTag.LESS || tokenTag == FSharpTokenTag.GREATER) return false;

      var tokenClass = token.CharClass;
      return tokenClass == FSharpTokenCharKind.String || tokenClass == FSharpTokenCharKind.Operator ||
             tokenClass == FSharpTokenCharKind.Comment || tokenClass == FSharpTokenCharKind.LineComment;
    }

    public object CurrentPosition { get; set; }
    public TokenNodeType TokenType { get; private set; }
    public int TokenStart { get; private set; }
    public int TokenEnd { get; private set; }
    public IBuffer Buffer { get; }
  }
}