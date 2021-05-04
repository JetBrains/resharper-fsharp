using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.Rd;
using JetBrains.ReSharper.Plugins.FSharp.Fantomas.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol
{
  public class FantomasConnection : ProtocolConnection<RdFantomasModel>
  {
    public FantomasConnection(Lifetime lifetime, RdFantomasModel protocolModel,
      IProtocol protocol, StartupOutputWriter startupOutputWriter, int processId, ISignal<int> processUnexpectedExited)
      : base(lifetime, protocolModel, protocol, startupOutputWriter, processId, processUnexpectedExited)
    {
    }
  }
}
