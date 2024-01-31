using System;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Impl.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;

// todo: disabled until reference on argument name to corresponding parameter is implemented

//[Language(typeof(FSharpLanguage))]
public class FSharpNamedArgumentLanguageService : INamedArgumentLanguageService
{
  public string[] GetPossibleNamedArguments(IPsiSourceFile sourceFile)
  {
    var file = sourceFile.GetDominantPsiFile<FSharpLanguage>();
    if (file is null) return EmptyArray<string>.Instance;

    var names = new FrugalLocalHashSet<string>();

    var lexer = file.CachingLexer;
    var identifierRange = TextRange.InvalidRange;
    var state = 0;

    for (lexer.Start(); lexer.TokenType != null; lexer.Advance())
    {
      if (lexer.TokenType.IsWhitespace || lexer.TokenType.IsComment) continue;

      if (lexer.TokenType == FSharpTokenType.COMMA || lexer.TokenType == FSharpTokenType.LPAREN)
      {
        state = 1;
        continue;
      }

      switch (state)
      {
        case 0:
        {
          continue;
        }

        case 1:
        {
          if (lexer.TokenType == FSharpTokenType.IDENTIFIER)
          {
            identifierRange = new TextRange(lexer.TokenStart, lexer.TokenEnd);
            state = 2;
          }
          else
          {
            state = 0;
          }

          continue;
        }

        case 2:
        {
          if (lexer.TokenType == FSharpTokenType.EQUALS)
          {
            names.Add(lexer.Buffer.GetText(identifierRange).RemoveBackticks());
          }

          state = 0;
          continue;
        }

        // ReSharper disable once UnreachableSwitchCaseDueToIntegerAnalysis
        default:
        {
          throw new InvalidOperationException("bad state:" + state);
        }
      }
    }

    return names.ToArray();
  }
}