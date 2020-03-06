using System;
using System.Linq;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedNamespace : IProxyProvidedNamespace
  {
    private readonly RdProvidedNamespace myProvidedNamespace;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;

    private RdProvidedNamespaceProcessModel RdProvidedNamespaceProcessModel =>
      myProcessModel.RdProvidedNamespaceProcessModel;

    private int EntityId => myProvidedNamespace.EntityId;

    public ProxyProvidedNamespace(RdProvidedNamespace providedNamespace, RdFSharpTypeProvidersLoaderModel processModel)
    {
      myProvidedNamespace = providedNamespace;
      myProcessModel = processModel;
    }

    public string NamespaceName => myProvidedNamespace.NamespaceName;

    public IProvidedNamespace[] GetNestedNamespaces() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedNamespaceProcessModel.GetNestedNamespaces
        .Sync(EntityId)
        .Select(t => new ProxyProvidedNamespace(t, myProcessModel))
        .ToArray();

    public Type[] GetTypes() =>
      throw new Exception("GetTypes should be unreachable");

    public RdProvidedType[] GetRdTypes() => RdProvidedNamespaceProcessModel.GetTypes.Sync(EntityId);

    public Type ResolveTypeName(string typeName) =>
      throw new Exception("ResolveTypeName should be unreachable");

    public RdProvidedType ResolveRdTypeName(string typeName) =>
      RdProvidedNamespaceProcessModel.ResolveTypeName.Sync(new ResolveTypeNameArgs(EntityId, typeName));
  }
}
