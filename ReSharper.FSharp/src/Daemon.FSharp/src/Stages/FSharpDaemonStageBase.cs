using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Psi;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
{
  public abstract class FSharpDaemonStageBase : IDaemonStage
  {
    public static readonly Key<FSharpOption<FSharpCheckFileResults>> TypeCheckResults =
      new Key<FSharpOption<FSharpCheckFileResults>>("FSharpTypeCheckResults");

    protected virtual bool IsSupported(IPsiSourceFile sourceFile, DaemonProcessKind processKind)
    {
      if (sourceFile == null || !sourceFile.IsValid()) return false;
      return sourceFile.IsLanguageSupported<FSharpLanguage>() && !sourceFile.Properties.IsNonUserFile;
    }

    public IEnumerable<IDaemonStageProcess> CreateProcess(IDaemonProcess daemonProcess,
      IContextBoundSettingsStore settings, DaemonProcessKind processKind)
    {
      if (!IsSupported(daemonProcess.SourceFile, processKind)) return EmptyList<IDaemonStageProcess>.InstanceList;

      if (!(daemonProcess.SourceFile.GetPrimaryPsiFile() is IFSharpFile fsFile))
        return EmptyList<IDaemonStageProcess>.Instance;

      var process = CreateProcess(fsFile, daemonProcess);
      return process != null
        ? new[] {process}
        : EmptyList<IDaemonStageProcess>.InstanceList;
    }

    [CanBeNull]
    protected abstract IDaemonStageProcess CreateProcess([NotNull] IFSharpFile fsFile,
      [NotNull] IDaemonProcess process);
  }
}