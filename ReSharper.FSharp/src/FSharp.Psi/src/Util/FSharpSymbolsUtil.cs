using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
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
      var checkResults = fsFile.GetParseAndCheckResults(false)?.Value.CheckResults;
      if (checkResults == null)
        return null;

      var coords = sourceFile.Document.GetCoordsByOffset(offset);
      var lineText = sourceFile.Document.GetLineText(coords.Line);
      var findSymbolAsync =
        checkResults.GetSymbolUseAtLocation((int) coords.Line + 1, (int) coords.Column, lineText, names,
          FSharpOption<string>.None);
      try
      {
        return FSharpAsync.RunSynchronously(findSymbolAsync, FSharpOption<int>.Some(1000), null)?.Value.Symbol;
      }
      catch (Exception) // Cannot access FCS internal exception types here
      {
        return null; // internal FCS or type provider error
      }
    }

    public static bool IsOpGreaterThan([NotNull] this FSharpSymbol symbol)
    {
      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      return mfv != null && mfv.CompiledName.Equals("op_GreaterThan", StringComparison.Ordinal);
    }

    [CanBeNull]
    public static FSharpMemberOrFunctionOrValue TryGetPropertyFromAccessor(
      [NotNull] this FSharpMemberOrFunctionOrValue mfv)
    {
      if (mfv.IsProperty)
        return mfv;

      var members = mfv.EnclosingEntity?.Value.MembersFunctionsAndValues;
      return mfv.IsModuleValueOrMember
        ? members?.FirstOrDefault(m => m.IsProperty && m.DisplayName == mfv.DisplayName) ?? mfv
        : mfv;
    }
  }
}