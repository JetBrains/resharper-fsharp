using JetBrains.Rd.Base;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  internal interface IOutOfProcessHost<in T> where T : RdBindableBase
  {
    void Initialize(T model);
  }
}
