using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Rd.Base;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public abstract class OutOfProcessProtocolManagerBase<T, TU> : IOutOfProcessProtocolManager<T, TU>
    where TU : RdBindableBase
  {
    private readonly IDictionary<T, TU> processModelsCache = new Dictionary<T, TU>();

    [ContractAnnotation("providedNativeModel:null => null")]
    public TU Register(T providedNativeModel, ITypeProvider providedModelOwner)
    {
      if (providedNativeModel == null) return null;

      if (!processModelsCache.TryGetValue(providedNativeModel, out var processModel))
      {
        processModel = CreateProcessModel(providedNativeModel, providedModelOwner);
        processModelsCache.Add(providedNativeModel, processModel);
      }

      return processModel;
    }

    protected abstract TU CreateProcessModel(T providedNativeModel, ITypeProvider providedModelOwner);
  }
}
