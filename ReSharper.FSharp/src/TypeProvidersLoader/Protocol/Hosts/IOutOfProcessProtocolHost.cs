using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  public interface IOutOfProcessProtocolHost<in T, out TU> where TU : class
  {
    [ContractAnnotation("null => null")]
    TU GetRdModel([CanBeNull] T providedNativeModel);
  }
}
