using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Quotations;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedVarWithCache : ProvidedVar, IRdProvidedEntity
  {
    private readonly RdProvidedVar myVar;
    private readonly ProvidedTypeContext myContext;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ITypeProviderCache myCache;
    
    public int EntityId => myVar.EntityId;
    private RdProvidedVarProcessModel RdProvidedVarProcessModel => myProcessModel.RdProvidedVarProcessModel;

    private ProxyProvidedVarWithCache(RdProvidedVar var, ProvidedTypeContext context,
      RdFSharpTypeProvidersLoaderModel processModel, ITypeProviderCache cache) : base(
      FSharpVar.Global("__fake__", typeof(string)), context)
    {
      myVar = var;
      myContext = context;
      myProcessModel = processModel;
      myCache = cache;
    }

    [ContractAnnotation("var:null => null")]
    public static ProxyProvidedVarWithCache Create(RdProvidedVar var,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, ITypeProviderCache cache) =>
      var == null ? null : new ProxyProvidedVarWithCache(var, context, processModel, cache);

    public override string Name => myVar.Name;
    public override bool IsMutable => myVar.IsMutable;

    public override ProvidedType Type =>
      myCache.GetOrCreateWithContext(myTypeId ??= RdProvidedVarProcessModel.Type.Sync(EntityId), myContext);

    //TODO: reduce allocations count
    public override bool Equals(object obj) => obj switch
    {
      ProvidedVar y => Type.FullName + Name == y.Type.FullName + y.Name,
      _ => false
    };

    public override int GetHashCode() => (Type.FullName + Name).GetHashCode();

    private int? myTypeId;
  }
}
