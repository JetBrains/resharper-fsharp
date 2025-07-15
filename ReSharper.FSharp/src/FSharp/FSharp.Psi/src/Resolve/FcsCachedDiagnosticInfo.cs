using System;
using FSharp.Compiler.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;

public class FcsCachedDiagnosticInfo(FSharpDiagnostic fcsDiagnostic, IFSharpFile fsFile, int offset)
{
  private WeakReference<FSharpDiagnostic> myReference = new(fcsDiagnostic);

  public FSharpDiagnostic GetDiagnostic()
  {
    if (myReference.TryGetTarget(out var diagnostic))
      return diagnostic;

    diagnostic = fsFile.FcsCapturedInfo.GetDiagnostic(offset);
    myReference = new(diagnostic);
    return diagnostic;
  }

  public ExtendedData.TypeMismatchDiagnosticExtendedData TypeMismatchData =>
    GetDiagnostic()?.ExtendedData?.Value as ExtendedData.TypeMismatchDiagnosticExtendedData;

  public static bool CanBeCached(FSharpDiagnostic diagnostic) =>
    diagnostic.ErrorNumber == 1;
}
