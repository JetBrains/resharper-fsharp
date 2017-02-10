using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler;

namespace JetBrains.ReSharper.Daemon.FSharp.Stages
{
  [DaemonStage(StagesBefore = new[] {typeof(GlobalFileStructureCollectorStage)})]
  public class SyntaxErrorsStage : FSharpDaemonStageBase
  {
    protected override IDaemonStageProcess CreateProcess(IFSharpFile fsFile, IDaemonProcess process)
    {
      var errors = fsFile.ParseResults?.Errors ?? EmptyArray<FSharpErrorInfo>.Instance;
      return new ErrorsStageProcess(process, errors);
    }

    public override ErrorStripeRequest NeedsErrorStripe(IPsiSourceFile sourceFile,
      IContextBoundSettingsStore settingsStore)
    {
      return ErrorStripeRequest.STRIPE_AND_ERRORS;
    }
  }
}