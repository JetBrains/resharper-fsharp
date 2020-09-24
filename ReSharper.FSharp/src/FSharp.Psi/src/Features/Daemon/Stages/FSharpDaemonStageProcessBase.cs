using System;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
{
  [AllowNullLiteral]
  public abstract class FSharpDaemonStageProcessBase : TreeNodeVisitor<IHighlightingConsumer>, IDaemonStageProcess
  {
    private const int InterruptCheckTime = 20;
    protected readonly SeldomInterruptCheckerWithCheckTime SeldomInterruptChecker;

    [NotNull] public IFSharpFile FSharpFile;
    [NotNull] public IModuleReferenceResolveContext ResolveContext { get; }

    protected FSharpDaemonStageProcessBase(IFSharpFile fsFile, IDaemonProcess daemonProcess)
    {
      FSharpFile = fsFile;
      DaemonProcess = daemonProcess;
      ResolveContext = fsFile.GetResolveContext();
      SeldomInterruptChecker = new SeldomInterruptCheckerWithCheckTime(InterruptCheckTime);
    }

    public IDaemonProcess DaemonProcess { get; }
    public abstract void Execute(Action<DaemonStageResult> committer);
  }
}
