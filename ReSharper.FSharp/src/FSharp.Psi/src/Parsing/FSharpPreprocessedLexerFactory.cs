using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using Microsoft.FSharp.Collections;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public class FSharpPreprocessedLexerFactory : ILexerFactory
  {
    private readonly HashSet<string> myDefinedConstant;
    private readonly FSharpPreprocessor myPreprocessor = new FSharpPreprocessor();

    public FSharpPreprocessedLexerFactory(FSharpList<string> definedConstants) =>
      myDefinedConstant = new HashSet<string>(definedConstants);

    public ILexer CreateLexer(IBuffer buffer) =>
      new FSharpPreprocessedLexer(buffer, myPreprocessor, myDefinedConstant);

    public ILexer CreateLexer(ILexer lexer) =>
      new FSharpPreprocessedLexer(lexer, myPreprocessor, myDefinedConstant);
  }
}
