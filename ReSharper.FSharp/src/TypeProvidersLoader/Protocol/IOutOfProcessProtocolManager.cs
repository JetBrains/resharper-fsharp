using JetBrains.Annotations;
using JetBrains.Rd.Base;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public interface IOutOfProcessProtocolManager<in T, out TU> where TU : RdBindableBase
  {
    TU Register([CanBeNull] T providedNativeModel, [NotNull] ITypeProvider providedModelOwner);
  }
}
