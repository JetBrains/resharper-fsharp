using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Psi.FSharp.Parsing
{
  internal class FSharpLexerFactory : ILexerFactory
  {
    private readonly IPsiSourceFile mySourceFile;
    private readonly FSharpCheckerService myFSharpCheckerService;

    public FSharpLexerFactory(IPsiSourceFile sourceFile, FSharpCheckerService fSharpCheckerService)
    {
      mySourceFile = sourceFile;
      myFSharpCheckerService = fSharpCheckerService;
    }

    public ILexer CreateLexer(IBuffer buffer)
    {
      return new FSharpLexer(mySourceFile.Document, myFSharpCheckerService.GetDefinedConstants(mySourceFile));
    }
  }
}