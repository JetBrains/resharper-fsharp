using FSharp.ExternalFormatter.Protocol;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.Platform.RdFramework.ExternalProcess.Util;
using JetBrains.Rd.Impl;
using JetBrains.Rider.FSharp.ExternalFormatter.Client;
using JetBrains.Util;
using JetBrains.Rd.Tasks;

namespace FSharp.ExternalFormatter.Host
{
  internal class ExternalFormatterEndPoint : ProtocolEndPoint<RdFSharpExternalFormatterModel, RdSimpleDispatcher>
  {
    private readonly FantomasCodeFormatter myCodeFormatter;
    protected override string ProtocolName => "External Formatter Host";

    public ExternalFormatterEndPoint() : base(FantomasProtocolConstants.PARENT_PROCESS_PID_ENV_VARIABLE)
    {
      myCodeFormatter = new FantomasCodeFormatter();
    }

    protected override RdSimpleDispatcher InitDispatcher(Lifetime lifetime, ILogger logger) =>
      new RdSimpleDispatcher(lifetime, logger);

    protected override void InitLogger(Lifetime lifetime, string path) =>
      ProtocolEndPointUtil.InitLogger(path, lifetime, LoggingLevel.TRACE);

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
