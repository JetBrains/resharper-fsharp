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

    private static RdProvidedType CreateRdModelInternal(ProvidedType providedModel, int entityId)
    {
      // We cannot request these properties from GenericParameter due to their implementation in .NET
      var isValueType = !providedModel.IsGenericParameter && providedModel.IsValueType;
      var isClass = !providedModel.IsGenericParameter && providedModel.IsClass;

      var flags = RdProvidedTypeFlags.None;
      if (isClass) flags |= RdProvidedTypeFlags.IsClass;
      if (isValueType) flags |= RdProvidedTypeFlags.IsValueType;
      if (providedModel.IsVoid) flags |= RdProvidedTypeFlags.IsVoid;
      if (providedModel.IsEnum) flags |= RdProvidedTypeFlags.IsEnum;
      if (providedModel.IsByRef) flags |= RdProvidedTypeFlags.IsByRef;
      if (providedModel.IsSealed) flags |= RdProvidedTypeFlags.IsSealed;
      if (providedModel.IsPublic) flags |= RdProvidedTypeFlags.IsPublic;
      if (providedModel.IsErased) flags |= RdProvidedTypeFlags.IsErased;
      if (providedModel.IsMeasure) flags |= RdProvidedTypeFlags.IsMeasure;
      if (providedModel.IsPointer) flags |= RdProvidedTypeFlags.IsPointer;
      if (providedModel.IsAbstract) flags |= RdProvidedTypeFlags.IsAbstract;
      if (providedModel.IsInterface) flags |= RdProvidedTypeFlags.IsInterface;
      if (providedModel.IsGenericType) flags |= RdProvidedTypeFlags.IsGenericType;
      if (providedModel.IsNestedPublic) flags |= RdProvidedTypeFlags.IsNestedPublic;
      if (providedModel.IsSuppressRelocate) flags |= RdProvidedTypeFlags.IsSuppressRelocate;
      if (providedModel.IsGenericParameter) flags |= RdProvidedTypeFlags.IsGenericParameter;

      return new RdProvidedType(providedModel.FullName, providedModel.Namespace, flags, providedModel.Name, entityId);
    }

    protected int CreateEntityKey(ProvidedType providedModel)
    {
      Interlocked.Increment(ref myCurrentId);
      return myCurrentId;
    }
  }
}
