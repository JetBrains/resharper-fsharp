using JetBrains.Annotations;
using JetBrains.Rd.Base;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public interface IOutOfProcessProtocolHost<in T, out TU> where TU : RdBindableBase
  {
    TU GetRdModel([CanBeNull] T providedNativeModel, [NotNull] ITypeProvider providedModelOwner);
  }
}
