using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class ProvidedTypeRdModelsCreator : IProvidedRdModelsCreator<ProvidedType, RdProvidedType>
  {
    private readonly IProvidedCache<Tuple<ProvidedType, RdProvidedType, int>> myCache;
    private readonly IDictionary<ProvidedType, int> myIdsCache;
    private int myCurrentId;

    public ProvidedTypeRdModelsCreator(IProvidedCache<Tuple<ProvidedType, RdProvidedType, int>> cache,
      IEqualityComparer<ProvidedType> equalityComparer)
    {
      myCache = cache;
      myIdsCache = new Dictionary<ProvidedType, int>(equalityComparer);
    }

    [ContractAnnotation("providedModel:null => null")]
    public RdProvidedType CreateRdModel(ProvidedType providedModel, int typeProviderId)
    {
      if (providedModel == null) return null;

      if (myIdsCache.TryGetValue(providedModel, out var id))
      {
        var (_, rdModel, _) = myCache.Get(id);
        return rdModel;
      }

      id = CreateEntityKey(providedModel);
      var model = CreateRdModelInternal(providedModel, id);

      myCache.Add(id, new Tuple<ProvidedType, RdProvidedType, int>(providedModel, model, typeProviderId));
      myIdsCache.Add(providedModel, id);

      return model;
    }

    private static RdProvidedType CreateRdModelInternal(ProvidedType providedNativeModel, int entityId) =>
      new RdProvidedType(providedNativeModel.FullName,
        providedNativeModel.Namespace, providedNativeModel.IsVoid,
        providedNativeModel.IsGenericParameter, providedNativeModel.IsValueType, providedNativeModel.IsByRef,
        providedNativeModel.IsPointer, providedNativeModel.IsEnum, providedNativeModel.IsArray,
        providedNativeModel.IsInterface, providedNativeModel.IsClass, providedNativeModel.IsSealed,
        providedNativeModel.IsAbstract, providedNativeModel.IsPublic, providedNativeModel.IsNestedPublic,
        providedNativeModel.IsSuppressRelocate, providedNativeModel.IsErased, providedNativeModel.IsGenericType,
        providedNativeModel.Name, entityId);

    protected int CreateEntityKey(ProvidedType providedModel)
    {
      Interlocked.Increment(ref myCurrentId);
      return myCurrentId;
    }
  }
}
