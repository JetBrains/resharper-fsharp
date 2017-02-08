using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

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
//      return new FSharpLexer(mySourceFile.Document, FSharpCheckerUtil.GetDefines(mySourceFile));
      return new FSharpLexer(mySourceFile.Document, EmptyArray<string>.Instance);
    }
  }
}