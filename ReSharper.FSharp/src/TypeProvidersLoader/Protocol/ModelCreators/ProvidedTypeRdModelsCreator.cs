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

    private static RdProvidedType CreateRdModelInternal(ProvidedType providedModel, int entityId) =>
      new RdProvidedType(providedModel.FullName,
        providedModel.Namespace, providedModel.IsVoid,
        providedModel.IsGenericParameter, providedModel.IsValueType, providedModel.IsByRef,
        providedModel.IsPointer, providedModel.IsEnum, providedModel.IsArray,
        providedModel.IsInterface, providedModel.IsClass, providedModel.IsSealed,
        providedModel.IsAbstract, providedModel.IsPublic, providedModel.IsNestedPublic,
        providedModel.IsSuppressRelocate, providedModel.IsErased, providedModel.IsGenericType,
        providedModel.IsMeasure, providedModel.Name, entityId);

    protected int CreateEntityKey(ProvidedType providedModel)
    {
      Interlocked.Increment(ref myCurrentId);
      return myCurrentId;
    }
  }
}
