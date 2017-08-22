using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
{
  [DaemonStage(StagesBefore = new[] {typeof(SyntaxHighlightingStage)},
    StagesAfter = new[] {typeof(SetResolvedSymbolsStage)})]
  public class SyntaxErrorsStage : FSharpDaemonStageBase
  {
    public class SyntaxErrorsStageProcess : ErrorsStageProcessBase
    {
      public SyntaxErrorsStageProcess([NotNull] IDaemonProcess process, [NotNull] FSharpErrorInfo[] errors)
        : base(process, errors)
      {
      }
    }

    protected override IDaemonStageProcess CreateProcess(IFSharpFile fsFile, IDaemonProcess process)
    {
      var errors = fsFile.ParseResults?.Value?.Errors ?? EmptyArray<FSharpErrorInfo>.Instance;
      return new SyntaxErrorsStageProcess(process, errors);
    }

    public override ErrorStripeRequest NeedsErrorStripe(IPsiSourceFile sourceFile,
      IContextBoundSettingsStore settingsStore)
    {
      return ErrorStripeRequest.STRIPE_AND_ERRORS;
    }
  }
}