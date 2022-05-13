using System.Linq;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache
{
  public class ProvidedTypesCache : ProvidedEntitiesCacheBase<ProxyProvidedType, int, ProvidedTypeContext>
  {
    private RdProvidedTypeProcessModel ProvidedTypeProcessModel =>
      TypeProvidersContext.Connection.ProtocolModel.RdProvidedTypeProcessModel;

    public ProvidedTypesCache(TypeProvidersContext typeProvidersContext) : base(typeProvidersContext)
    {
    }

    protected override bool KeyHasValue(int key) => key != ProvidedConst.DefaultId;

    protected override ProxyProvidedType
      Create(int key, IProxyTypeProvider typeProvider, ProvidedTypeContext context) =>
      ProxyProvidedType.Create(
        TypeProvidersContext.Connection.ExecuteWithCatch(() =>
          ProvidedTypeProcessModel.GetProvidedType.Sync(key, RpcTimeouts.Maximal)),
        typeProvider, TypeProvidersContext);

    protected override ProxyProvidedType[] CreateBatch(int[] keys, IProxyTypeProvider typeProvider,
      ProvidedTypeContext context) =>
      TypeProvidersContext.Connection.ExecuteWithCatch(() =>
          ProvidedTypeProcessModel.GetProvidedTypes.Sync(keys, RpcTimeouts.Maximal))
        .Select(t => ProxyProvidedType.Create(t, typeProvider, TypeProvidersContext))
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
