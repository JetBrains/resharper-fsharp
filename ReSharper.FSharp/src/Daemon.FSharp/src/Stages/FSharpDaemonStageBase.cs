using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
{
  public abstract class FSharpDaemonStageBase : IDaemonStage
  {
    protected virtual bool IsSupported(IPsiSourceFile sourceFile, DaemonProcessKind processKind) =>
      sourceFile != null && sourceFile.IsValid() &&
      sourceFile.LanguageType.Is<FSharpProjectFileType>() && !sourceFile.Properties.IsNonUserFile;

    public IEnumerable<IDaemonStageProcess> CreateProcess(IDaemonProcess daemonProcess,
      IContextBoundSettingsStore settings, DaemonProcessKind processKind)
    {
      if (!IsSupported(daemonProcess.SourceFile, processKind))
        return EmptyList<IDaemonStageProcess>.InstanceList;

      if (!(daemonProcess.SourceFile.GetPrimaryPsiFile() is IFSharpFile fsFile))
        return EmptyList<IDaemonStageProcess>.Instance;

      var process = CreateStageProcess(fsFile, settings, daemonProcess);
      return process != null
        ? new[] {process}
        : EmptyList<IDaemonStageProcess>.InstanceList;
    }

    [CanBeNull]
    protected abstract IDaemonStageProcess CreateStageProcess([NotNull] IFSharpFile fsFile,
      IContextBoundSettingsStore settings, [NotNull] IDaemonProcess process);
  }
}
