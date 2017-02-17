using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;

namespace JetBrains.ReSharper.Psi.FSharp.Impl
{
  public class FSharpUnresolvedReference : FSharpReferenceBase
  {
    public FSharpUnresolvedReference([NotNull] FSharpIdentifierToken owner) : base(owner)
    {
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
      var symbol = FindFSharpSymbol();
      if (symbol == null) return ResolveResultWithInfo.Ignore;

      var fakeElement = new FSharpFakeElementFromReference(symbol, myOwner);
      return new ResolveResultWithInfo(new SimpleResolveResult(fakeElement), ResolveErrorType.OK);
    }

    [CanBeNull]
    private FSharpSymbol FindFSharpSymbol()
    {
      var sourceFile = myOwner.GetSourceFile();
      Assertion.AssertNotNull(sourceFile, "sourceFile != null");
      var checkResults = (myOwner.GetContainingFile() as IFSharpFile)?.GetCheckResults();
      if (checkResults == null) return null;

      var coords = sourceFile.Document.GetCoordsByOffset(myOwner.GetTreeEndOffset().Offset);
      var lineText = sourceFile.Document.GetLineText(coords.Line);
      var names = ListModule.OfArray(new[] {myOwner.GetText()});
      try
      {
        var findSymbolAsync =
          checkResults.GetSymbolUseAtLocation((int) coords.Line + 1, (int) coords.Column, lineText, names);
        return FSharpAsync.RunSynchronously(findSymbolAsync, null, null)?.Value.Symbol;
      }
      catch (ErrorLogger.UnresolvedPathReferenceNoRange)
      {
        return null; // internal FCS error
      }
    }
  }
}