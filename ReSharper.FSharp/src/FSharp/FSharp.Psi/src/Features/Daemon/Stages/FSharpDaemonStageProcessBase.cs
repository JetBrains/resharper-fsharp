using System;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
{
  [AllowNullLiteral]
  public abstract class FSharpDaemonStageProcessBase(IFSharpFile fsFile, IDaemonProcess daemonProcess)
    : TreeNodeVisitor<IHighlightingConsumer>, IDaemonStageProcess
  {
    [NotNull] public IFSharpFile FSharpFile = fsFile;
    [NotNull] public IModuleReferenceResolveContext ResolveContext { get; } = fsFile.GetResolveContext();

    public IDaemonProcess DaemonProcess { get; } = daemonProcess;
    public abstract void Execute(Action<DaemonStageResult> committer);
  }
}
