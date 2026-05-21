using System;
using FSharp.Compiler.Diagnostics;
using FSharp.Compiler.Text;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ContentModel;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;

public class FcsCachedDiagnosticInfo(FSharpDiagnostic diagnostic, IFSharpFile fsFile, Position pos)
  : ISupportsContentModelForkTranslation
{
  private WeakReference<FSharpDiagnostic> myReference = new(diagnostic);

  public FSharpDiagnostic GetDiagnostic()
  {
    if (myReference.TryGetTarget(out var diagnostic))
      return diagnostic;

    diagnostic = fsFile.FcsCapturedInfo.GetDiagnostic(pos);
    myReference = new(diagnostic);
    return diagnostic;
  }

  public ExtendedData.TypeMismatchDiagnosticExtendedData TypeMismatchData =>
    GetDiagnostic().NotNull().ExtendedData?.Value as ExtendedData.TypeMismatchDiagnosticExtendedData;

  public static bool CanBeCached(FSharpDiagnostic diagnostic) =>
    diagnostic.ErrorNumber is 1 or 193;

  object ISupportsContentModelForkTranslation.TryTranslateToCurrentFork(IContentModelForkTranslator translator)
  {
    if (translator.TryTranslateAnythingToCurrentFork(fsFile) is IFSharpFile fsFileInFork)
      return new FcsCachedDiagnosticInfo(diagnostic, fsFileInFork, pos);

    return null;
  }
}
