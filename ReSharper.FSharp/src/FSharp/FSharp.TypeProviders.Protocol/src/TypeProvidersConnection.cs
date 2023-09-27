using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.Rd;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  public class TypeProvidersConnection : ProtocolConnection<RdFSharpTypeProvidersModel>
  {
    public TypeProvidersConnection(Lifetime lifetime, RdFSharpTypeProvidersModel protocolModel,
      IProtocol protocol, StartupOutputWriter startupOutputWriter, int processId, ISignal<int> processUnexpectedExited)
      : base(lifetime, protocolModel, protocol, startupOutputWriter, processId, processUnexpectedExited)
    {
    }
  }
}
