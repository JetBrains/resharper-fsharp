using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.Rd;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol
{
  public class TypeProvidersConnection : ProtocolConnection<RdFSharpTypeProvidersLoaderModel>
  {
    public TypeProvidersConnection(Lifetime lifetime, RdFSharpTypeProvidersLoaderModel protocolModel,
      IProtocol protocol, StartupOutputWriter startupOutputWriter, int processId, ISignal<int> processUnexpectedExited)
      : base(lifetime, protocolModel, protocol, startupOutputWriter, processId, processUnexpectedExited)
    {
    }
  }
}
