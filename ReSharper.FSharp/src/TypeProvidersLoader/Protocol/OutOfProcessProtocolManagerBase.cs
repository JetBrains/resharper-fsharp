using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Rd.Base;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public abstract class OutOfProcessProtocolManagerBase<T, TU> : IOutOfProcessProtocolManager<T, TU>
    where TU : RdBindableBase
  {
    private readonly IDictionary<T, TU> myProcessModelsCache;

    protected OutOfProcessProtocolManagerBase(IEqualityComparer<T> equalityComparer)
    {
      myProcessModelsCache = new Dictionary<T, TU>(equalityComparer);
    }

    [ContractAnnotation("providedNativeModel:null => null")]
    public TU Register(T providedNativeModel, ITypeProvider providedModelOwner)
    {
      if (providedNativeModel == null) return null;

      if (!myProcessModelsCache.TryGetValue(providedNativeModel, out var processModel))
      {
        processModel = CreateProcessModel(providedNativeModel, providedModelOwner);
        myProcessModelsCache.Add(providedNativeModel, processModel);
      }

      return processModel;
    }

    protected abstract TU CreateProcessModel(T providedNativeModel, ITypeProvider providedModelOwner);
  }
}
