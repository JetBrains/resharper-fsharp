using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
{
  public abstract class FSharpDaemonStageBase(bool visibleDocumentsOnly = false, bool enableInSignatures = true) : IDaemonStage
  {
    protected virtual bool IsSupported(IPsiSourceFile sourceFile, DaemonProcessKind processKind) =>
      (!visibleDocumentsOnly || processKind == DaemonProcessKind.VISIBLE_DOCUMENT) &&
      sourceFile != null && sourceFile.IsValid() &&
      sourceFile.Properties is { IsNonUserFile: false, ProvidesCodeModel: true } &&
      sourceFile.LanguageType.Is<FSharpProjectFileType>() &&
      (enableInSignatures || !sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>());

    public IEnumerable<IDaemonStageProcess> CreateProcess(IDaemonProcess daemonProcess,
      IContextBoundSettingsStore settings, DaemonProcessKind processKind)
    {
      if (!IsSupported(daemonProcess.SourceFile, processKind))
        return EmptyList<IDaemonStageProcess>.InstanceList;

      if (daemonProcess.SourceFile.GetPrimaryPsiFile() is not IFSharpFile fsFile)
        return EmptyList<IDaemonStageProcess>.Instance;

      var process = CreateStageProcess(fsFile, settings, daemonProcess, processKind);
      return process != null ? [process] : EmptyList<IDaemonStageProcess>.InstanceList;
    }

    [CanBeNull]
    protected abstract IDaemonStageProcess CreateStageProcess([NotNull] IFSharpFile fsFile,
      IContextBoundSettingsStore settings, [NotNull] IDaemonProcess process, DaemonProcessKind processKind);
  }
}
