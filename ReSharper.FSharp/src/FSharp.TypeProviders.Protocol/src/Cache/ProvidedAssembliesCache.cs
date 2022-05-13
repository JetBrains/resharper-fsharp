using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache
{
  public class ProvidedAssembliesCache : ProvidedEntitiesCacheBase<ProvidedAssembly, int, ProvidedTypeContext>
  {
    private RdProvidedAssemblyProcessModel ProvidedAssembliesProcessModel =>
      TypeProvidersContext.Connection.ProtocolModel.RdProvidedAssemblyProcessModel;

    public ProvidedAssembliesCache(TypeProvidersContext typeProvidersContext) : base(typeProvidersContext)
    {
    }

    protected override bool KeyHasValue(int key) => key != ProvidedConst.DefaultId;

    protected override ProvidedAssembly Create(int key, IProxyTypeProvider typeProvider, ProvidedTypeContext _)
      => ProxyProvidedAssembly.Create(
        TypeProvidersContext.Connection.ExecuteWithCatch(() =>
          ProvidedAssembliesProcessModel.GetProvidedAssembly.Sync(key)),
        TypeProvidersContext.Connection);

    protected override ProvidedAssembly[] CreateBatch(int[] keys, IProxyTypeProvider typeProvider, ProvidedTypeContext _)
      => throw new System.NotSupportedException();

    public override string Dump() =>
      "Provided Assemblies:\n" + string.Join("\n",
        EntitiesPerProvider
          .SelectMany(kvp => kvp.Value
            .Select(entityId => Entities[entityId])
            .Select(entity => (tpId: kvp.Key, Name: entity.GetLogName())))
          .OrderBy(t => t.Name)
          .Select(t => $"{t.tpId} {t.Name}"));
  }
}
