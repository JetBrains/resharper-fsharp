using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.LanguageService
{
  public class FSharpFakeParser : IParser
  {
    private readonly IPsiSourceFile mySourceFile;

    public FSharpFakeParser(IPsiSourceFile sourceFile)
    {
      mySourceFile = sourceFile;
    }

    public IFile ParseFile()
    {
      using (var lifetimeDefinition = Lifetimes.Define())
      {
        var parseResults = FSharpCheckerUtil.ParseFSharpFile(mySourceFile);
        InterruptableActivityCookie.CheckAndThrow();

        var defines = FSharpProjectOptionsProvider.GetDefinedConstants(mySourceFile);
        var tokenBuffer = new TokenBuffer(new FSharpLexer(mySourceFile.Document, defines));
        var treeBuilder = new FSharpFakeTreeBuilder(tokenBuffer.CreateLexer(), lifetimeDefinition.Lifetime);
        var fsFile = treeBuilder.CreateFakeFile() as IFSharpFile;
        Assertion.AssertNotNull(fsFile, "fsFile != null");
        fsFile.TokenBuffer = tokenBuffer;
        fsFile.ParseResults = parseResults;
        return fsFile;
      }
    }
  }
}