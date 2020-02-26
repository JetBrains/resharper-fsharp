using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader
{
  public interface ITypeProvidersLoader
  {
    ITypeProvider[] InstantiateTypeProvidersOfAssembly(InstantiateTypeProvidersOfAssemblyParameters parameters);
  }
}
