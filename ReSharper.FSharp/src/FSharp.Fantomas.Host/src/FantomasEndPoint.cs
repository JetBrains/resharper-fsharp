using System.Linq;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.Platform.RdFramework.ExternalProcess.Util;
using JetBrains.Rd.Impl;
using JetBrains.Util;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol;
using JetBrains.ReSharper.Plugins.FSharp.Fantomas.Server;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host
{
  internal class FantomasEndPoint : ProtocolEndPoint<RdFantomasModel, RdSimpleDispatcher>
  {
    protected override string ProtocolName => "Fantomas Host";

    public FantomasEndPoint() : base(FantomasProtocolConstants.PARENT_PROCESS_PID_ENV_VARIABLE)
    {
    }

    protected override RdSimpleDispatcher InitDispatcher(Lifetime lifetime, ILogger logger) => new(lifetime, logger);

    protected override void InitLogger(Lifetime lifetime, string path) =>
      ProtocolEndPointUtil.InitLogger(path, lifetime, LoggingLevel.TRACE);

    protected override RdFantomasModel InitModel(Lifetime lifetime, JetBrains.Rd.Impl.Protocol protocol)
    {
      var model = new RdFantomasModel(lifetime, protocol);

      model.GetFormatConfigFields.Set(GetFormatConfigFields);
      model.FormatSelection.Set(FormatSelection);
      model.FormatDocument.Set(FormatDocument);
      model.GetVersion.Set(GetVersion);
      model.Exit.Advise(lifetime, Terminate);

      return model;
    }

    private static string GetVersion(Unit _) => FantomasCodeFormatter.CurrentVersion.ToString();

    private static string[] GetFormatConfigFields(Unit _) =>
      FantomasCodeFormatter.FormatConfigFields.Select(t => t.Name).ToArray();

    private static string FormatSelection(RdFantomasFormatSelectionArgs args) =>
      FantomasCodeFormatter.FormatSelection(args);

    private static string FormatDocument(RdFantomasFormatDocumentArgs args) =>
      FantomasCodeFormatter.FormatDocument(args);

    protected override void Run(Lifetime lifetime, RdSimpleDispatcher dispatcher) => dispatcher.Run();
  }
}
