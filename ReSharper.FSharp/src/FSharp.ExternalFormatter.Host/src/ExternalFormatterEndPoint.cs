using FSharp.ExternalFormatter.Protocol;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.Rd.Impl;
using JetBrains.Rider.FSharp.ExternalFormatter.Client;
using JetBrains.Util;
using JetBrains.Util.Logging;
using JetBrains.Rd.Tasks;

namespace FSharp.ExternalFormatter.Host
{
  internal class ExternalFormatterEndPoint : ProtocolEndPoint<RdFSharpExternalFormatterModel, RdSimpleDispatcher>
  {
    private readonly ICodeFormatterProvider myCodeFormatter;
    protected override string ProtocolName => "External Formatter Host";

    public ExternalFormatterEndPoint() : base(ProtocolConstants.PARENT_PROCESS_PID_ENV_VARIABLE)
    {
      myCodeFormatter = new BundledCodeFormatter();
    }

    protected override RdSimpleDispatcher InitDispatcher(Lifetime lifetime, ILogger logger) =>
      new RdSimpleDispatcher(lifetime, logger);

    protected override void InitLogger(Lifetime lifetime, string path)
    {
      LogManager.Instance.SetConfig(new XmlLogConfigModel());
      var logPath = FileSystemPath.TryParse(path);
      if (logPath.IsNullOrEmpty()) return;

      var logEventListener = new FileLogEventListener(logPath);
      LogManager.Instance.AddOmnipresentLogger(lifetime, logEventListener, LoggingLevel.TRACE);
    }

    protected override RdFSharpExternalFormatterModel InitModel(Lifetime lifetime, JetBrains.Rd.Impl.Protocol protocol)
    {
      var model = new RdFSharpExternalFormatterModel(lifetime, protocol);

      model.FormatSelection.Set(FormatSelection);
      model.FormatDocument.Set(FormatDocument);
      model.Exit.Advise(lifetime, Terminate);

      return model;
    }

    private string FormatSelection(RdFormatSelectionArgs args) => myCodeFormatter.FormatSelection(args);
    private string FormatDocument(RdFormatDocumentArgs args) => myCodeFormatter.FormatDocument(args);

    protected override void Run(Lifetime lifetime, RdSimpleDispatcher dispatcher) => dispatcher.Run();
  }
}
