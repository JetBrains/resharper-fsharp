using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Psi.FSharp.Parsing
{
  internal class FSharpLexerFactory : ILexerFactory
  {
    private readonly IPsiSourceFile mySourceFile;

    public FSharpLexerFactory(IPsiSourceFile sourceFile)
    {
      mySourceFile = sourceFile;
    }

    public ILexer CreateLexer(IBuffer buffer)
    {
      return new FSharpLexer(mySourceFile.Document, FSharpProjectOptionsProvider.GetDefinedConstants(mySourceFile));
    }
  }
}