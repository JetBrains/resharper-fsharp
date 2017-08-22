using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Psi;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
{
  public abstract class FSharpDaemonStageBase : IDaemonStage
  {
    protected virtual bool IsSupported(IPsiSourceFile sourceFile)
    {
      if (sourceFile == null || !sourceFile.IsValid()) return false;
      return sourceFile.IsLanguageSupported<FSharpLanguage>() && !sourceFile.Properties.IsNonUserFile;
    }

    public IEnumerable<IDaemonStageProcess> CreateProcess(IDaemonProcess process,
      IContextBoundSettingsStore settings, DaemonProcessKind processKind)
    {
      if (!IsSupported(process.SourceFile)) return EmptyList<IDaemonStageProcess>.InstanceList;

      var fsFile = process.SourceFile.GetPrimaryPsiFile() as IFSharpFile;
      return fsFile != null ? new[] {CreateProcess(fsFile, process)} : EmptyList<IDaemonStageProcess>.InstanceList;
    }

    protected abstract IDaemonStageProcess CreateProcess([NotNull] IFSharpFile fsFile,
      [NotNull] IDaemonProcess process);

    public virtual ErrorStripeRequest NeedsErrorStripe(IPsiSourceFile sourceFile,
      IContextBoundSettingsStore settingsStore)
    {
      return ErrorStripeRequest.NONE;
    }
  }
}