using System.Diagnostics;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host
{
  internal class WriteToLogTraceListener : TraceListener
  {
    private readonly ILogger myLogger;
    public WriteToLogTraceListener(ILogger logger) => myLogger = logger;
    public override void Write(string message) => myLogger.Trace(message ?? "");
    public override void WriteLine(string message) => myLogger.Trace(message ?? "");
  }
}
