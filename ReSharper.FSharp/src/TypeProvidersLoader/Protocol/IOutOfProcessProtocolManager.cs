using JetBrains.Rd.Base;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public interface IOutOfProcessProtocolManager<in T, out TU> where TU : RdBindableBase
  {
    TU Register(T providedMethod);
  }
}
