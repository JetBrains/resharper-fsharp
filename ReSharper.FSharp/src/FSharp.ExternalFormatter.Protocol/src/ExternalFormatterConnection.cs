using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.Rd;
using JetBrains.Rider.FSharp.ExternalFormatter.Server;

namespace FSharp.ExternalFormatter.Protocol
{
  public class ExternalFormatterConnection : ProtocolConnection<RdFSharpExternalFormatterModel>
  {
    public ExternalFormatterConnection(Lifetime lifetime, RdFSharpExternalFormatterModel protocolModel,
      IProtocol protocol, StartupOutputWriter startupOutputWriter, int processId, ISignal<int> processUnexpectedExited)
      : base(lifetime, protocolModel, protocol, startupOutputWriter, processId, processUnexpectedExited)
    {
    }
  }
}
