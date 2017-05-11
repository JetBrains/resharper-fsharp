using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using Microsoft.FSharp.Collections;

namespace JetBrains.ReSharper.Psi.FSharp.Parsing
{
  internal class FSharpLexerFactory : ILexerFactory
  {
    private readonly IPsiSourceFile mySourceFile;
    private readonly FSharpList<string> myDefinedConstants;

    public FSharpLexerFactory(IPsiSourceFile sourceFile, FSharpList<string> definedConstants)
    {
      mySourceFile = sourceFile;
      myDefinedConstants = definedConstants;
    }

    public ILexer CreateLexer(IBuffer buffer)
    {
      return new FSharpLexer(mySourceFile.Document, myDefinedConstants);
    }
  }
}