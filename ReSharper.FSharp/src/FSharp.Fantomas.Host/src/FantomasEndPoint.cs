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
    private LifetimeDefinition myLoggerLifetime = Lifetime.Define(Lifetime.Terminated);
    private readonly FantomasCodeFormatter myCodeFormatter;
    private string myLoggingPath;
    protected override string ProtocolName => "Fantomas Host";

    public FantomasEndPoint() : base(FantomasProtocolConstants.PARENT_PROCESS_PID_ENV_VARIABLE) =>
      myCodeFormatter = new FantomasCodeFormatter();

    protected override RdSimpleDispatcher InitDispatcher(Lifetime lifetime, ILogger logger) =>
      new RdSimpleDispatcher(lifetime, logger);

    protected override void InitLogger(Lifetime lifetime, string path) => myLoggingPath = path;

    protected override RdFantomasModel InitModel(Lifetime lifetime, JetBrains.Rd.Impl.Protocol protocol)
    {
      var model = new RdFantomasModel(lifetime, protocol);
      model.EnableTracing.Advise(lifetime, enabled => ConfigureTracing(lifetime, enabled));

      model.GetFormatConfigFields.Set(GetFormatConfigFields);
      model.FormatSelection.Set(FormatSelection);
      model.FormatDocument.Set(FormatDocument);
      model.Exit.Advise(lifetime, Terminate);

      return model;
    }

    private static string[] GetFormatConfigFields(Unit _) =>
      FantomasCodeFormatter.FormatConfigFields.Select(t => t.Name).ToArray();

    private string FormatSelection(RdFantomasFormatSelectionArgs args) => myCodeFormatter.FormatSelection(args);
    private string FormatDocument(RdFantomasFormatDocumentArgs args) => myCodeFormatter.FormatDocument(args);

    protected override void Run(Lifetime lifetime, RdSimpleDispatcher dispatcher) => dispatcher.Run();

    private void ConfigureTracing(Lifetime lifetime, bool enable)
    {
      if (enable && myLoggerLifetime.Lifetime.IsNotAlive)
      {
        myLoggerLifetime = Lifetime.Define(lifetime);
        ProtocolEndPointUtil.InitLogger(myLoggingPath, myLoggerLifetime.Lifetime, LoggingLevel.TRACE);
      }

      else myLoggerLifetime.Terminate();
    }
  }
}
