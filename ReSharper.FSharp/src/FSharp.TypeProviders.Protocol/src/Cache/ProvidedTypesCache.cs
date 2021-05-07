using System.Linq;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache
{
  internal class ProvidedTypesCache : ProvidedEntitiesCacheBase<ProxyProvidedType>
  {
    private RdProvidedTypeProcessModel ProvidedTypeProcessModel =>
      TypeProvidersContext.Connection.ProtocolModel.RdProvidedTypeProcessModel;

    public ProvidedTypesCache(TypeProvidersContext typeProvidersContext) : base(typeProvidersContext)
    {
    }

    protected override ProxyProvidedType Create(int key, int typeProviderId,
      ProvidedTypeContextHolder context) =>
      ProxyProvidedType.Create(
        TypeProvidersContext.Connection.ExecuteWithCatch(() =>
          ProvidedTypeProcessModel.GetProvidedType.Sync(key, RpcTimeouts.Maximal)),
        typeProviderId, TypeProvidersContext, context ?? ProvidedTypeContextHolder.Create());

    protected override ProxyProvidedType[] CreateBatch(int[] keys, int typeProviderId,
      ProvidedTypeContextHolder context) =>
      TypeProvidersContext.Connection.ExecuteWithCatch(() =>
          ProvidedTypeProcessModel.GetProvidedTypes.Sync(keys, RpcTimeouts.Maximal))
        .Select(t => ProxyProvidedType.Create(t, typeProviderId, TypeProvidersContext,
          context ?? ProvidedTypeContextHolder.Create()))
        .ToArray();

    public override string Dump() =>
      "Provided Types:\n" + string.Join("\n",
        EntitiesPerProvider
          .SelectMany(kvp => kvp.Value
            .Select(entityId => Entities[entityId])
            .Select(entity => (tpId: kvp.Key, entity)))
          .OrderBy(t => t.entity.FullName)
          .Select(t => $"{t.tpId} {t.entity.FullName} (from {t.entity.Assembly.GetLogName()})"));
  }
}
