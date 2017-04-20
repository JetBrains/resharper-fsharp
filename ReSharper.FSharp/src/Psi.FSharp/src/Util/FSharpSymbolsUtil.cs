using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  public class FSharpSymbolsUtil
  {
    [CanBeNull]
    public static FSharpSymbol TryFindFSharpSymbol([NotNull] IFSharpFile fsFile, [NotNull] string name, int offset)
    {
      return TryFindFSharpSymbol(fsFile, ListModule.OfArray(new[] {name}), offset);
    }

    [CanBeNull]
    public static FSharpSymbol TryFindFSharpSymbol([NotNull] IFSharpFile fsFile, [NotNull] FSharpList<string> names,
      int offset)
    {
      var sourceFile = fsFile.GetSourceFile();
      Assertion.AssertNotNull(sourceFile, "sourceFile != null");
      var checkResults = fsFile.GetCheckResults();
      if (checkResults == null)
        return null;

      var coords = sourceFile.Document.GetCoordsByOffset(offset);
      var lineText = sourceFile.Document.GetLineText(coords.Line);
      var findSymbolAsync =
        checkResults.GetSymbolUseAtLocation((int) coords.Line + 1, (int) coords.Column, lineText, names);
      try
      {
        return FSharpAsync.RunSynchronously(findSymbolAsync, null, null)?.Value.Symbol;
      }
      catch (Exception) // Cannot access FCS internal exception types here
      {
        return null; // internal FCS or type provider error
      }
    }

    public static bool IsOpGreaterThan(FSharpSymbol symbol)
    {
      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      return mfv?.CompiledName == "op_GreaterThan";
    }
  }
}