using JetBrains.Rd.Base;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public interface IOutOfProcessHostFactory<out T> where T : RdBindableBase
  {
    T CreateProcessModel();
  }
}
