using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Exceptions;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache
{
  internal class ProvidedAssembliesCache : ProvidedEntitiesCacheBase<ProvidedAssembly>
  {
    private RdProvidedAssemblyProcessModel ProvidedAssembliesProcessModel =>
      TypeProvidersContext.Connection.ProtocolModel.RdProvidedAssemblyProcessModel;

    public ProvidedAssembliesCache(TypeProvidersContext typeProvidersContext) : base(typeProvidersContext)
    {
    }

    protected override ProvidedAssembly Create(int key, int typeProviderId, ProvidedTypeContextHolder context)
      => ProxyProvidedAssembly.Create(
        TypeProvidersContext.Connection.ExecuteWithCatch(() =>
          ProvidedAssembliesProcessModel.GetProvidedAssembly.Sync(key)),
        TypeProvidersContext.Connection);

    protected override ProvidedAssembly[] CreateBatch(int[] keys, int typeProviderId, ProvidedTypeContextHolder context)
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
