using JetBrains.Rd.Base;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public interface IOutOfProcessHostFactory<in T> where T : RdBindableBase
  {
    void Initialize(T model);
  }
}
