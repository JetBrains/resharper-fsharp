using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Psi.FSharp.Util
{
  public static class FSharpSymbolsUtil
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
      var checkResults = fsFile.GetParseAndCheckResults()?.Value.CheckResults;
      if (checkResults == null)
        return null;

      var coords = sourceFile.Document.GetCoordsByOffset(offset);
      var lineText = sourceFile.Document.GetLineText(coords.Line);
      var findSymbolAsync =
        checkResults.GetSymbolUseAtLocation((int) coords.Line + 1, (int) coords.Column, lineText, names);
      try
      {
        return FSharpAsync.RunSynchronously(findSymbolAsync, FSharpOption<int>.Some(1000), null)?.Value.Symbol;
      }
      catch (TimeoutException)
      {
        Logger.LogError("Getting symbol at location: {0}: {1}", sourceFile.GetLocation().FullPath, coords);
        return null;
      }
      catch (Exception) // Cannot access FCS internal exception types here
      {
        return null; // internal FCS or type provider error
      }
    }

    public static bool IsOpGreaterThan([NotNull] this FSharpSymbol symbol)
    {
      return (symbol as FSharpMemberOrFunctionOrValue)?.CompiledName == "op_GreaterThan";
    }

    [CanBeNull]
    public static FSharpMemberOrFunctionOrValue TryGetPropertyFromAccessor(
      [NotNull] this FSharpMemberOrFunctionOrValue mfv)
    {
      if (mfv.IsProperty)
        return mfv;

      var members = mfv.EnclosingEntity.MembersFunctionsAndValues;
      return mfv.IsModuleValueOrMember
        ? members.FirstOrDefault(m => m.IsProperty && m.DisplayName == mfv.DisplayName) ?? mfv
        : mfv;
    }
  }
}