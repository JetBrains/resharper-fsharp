using System;
using FSharp.Compiler.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;

public class FcsCachedDiagnosticInfo(FSharpDiagnostic diagnostic, IFSharpFile fsFile, DocumentRange documentRange)
{
  private WeakReference<FSharpDiagnostic> myReference = new(diagnostic);

  public readonly int Offset = documentRange.StartOffset.Offset;

  public FSharpDiagnostic GetDiagnostic()
  {
    if (myReference.TryGetTarget(out var diagnostic))
      return diagnostic;

    diagnostic = fsFile.FcsCapturedInfo.GetDiagnostic(Offset);
    myReference = new(diagnostic);
    return diagnostic;
  }

  public ExtendedData.TypeMismatchDiagnosticExtendedData TypeMismatchData =>
    GetDiagnostic()?.ExtendedData?.Value as ExtendedData.TypeMismatchDiagnosticExtendedData;

  public static bool CanBeCached(FSharpDiagnostic diagnostic) =>
    diagnostic.ErrorNumber == 1;
}
