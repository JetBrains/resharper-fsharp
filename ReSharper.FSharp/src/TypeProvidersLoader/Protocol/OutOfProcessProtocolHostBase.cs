using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.Rd.Base;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public abstract class OutOfProcessProtocolHostBase<T, TU> : IOutOfProcessProtocolHost<T, TU>
    where TU : RdBindableBase
  {
    protected readonly IDictionary<int, T> ProvidedModelsCache; // just base version
    private readonly IDictionary<T, int> myIdsCache;            // TODO: rewrite
    private int myCurrentId;

    protected OutOfProcessProtocolHostBase(IEqualityComparer<T> equalityComparer)
    {
      myIdsCache = new Dictionary<T, int>(equalityComparer);
      ProvidedModelsCache = new Dictionary<int, T>();
    }

    [ContractAnnotation("providedNativeModel:null => null")]
    public TU GetRdModel(T providedNativeModel, ITypeProvider providedModelOwner)
    {
      if (providedNativeModel == null) return null;

      if (!myIdsCache.TryGetValue(providedNativeModel, out var id))
      {
        id = CreateEntityId(providedNativeModel);
        myIdsCache.Add(providedNativeModel, id);
        ProvidedModelsCache.Add(id, providedNativeModel);
      }

      var rdModel = CreateRdModel(providedNativeModel, providedModelOwner);
      return rdModel;
    }

    protected virtual int CreateEntityId(T providedNativeModel)
    {
      Interlocked.Increment(ref myCurrentId);
      return myCurrentId;
    }

    protected abstract TU CreateRdModel(T providedNativeModel, ITypeProvider providedModelOwner);
  }
}
