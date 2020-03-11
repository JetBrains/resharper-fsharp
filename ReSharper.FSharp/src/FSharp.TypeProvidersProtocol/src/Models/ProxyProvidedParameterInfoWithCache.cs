using System.Linq;
using JetBrains.Annotations;
using JetBrains.Core;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedParameterInfoWithCache : ProvidedParameterInfo
  {
    private readonly RdProvidedParameterInfo myParameterInfo;
    private readonly ProvidedTypeContext myContext;
    private readonly ITypeProviderCache myCache;

    public ProxyProvidedParameterInfoWithCache(RdProvidedParameterInfo parameterInfo, ProvidedTypeContext context,
      ITypeProviderCache cache) : base(
      typeof(string).GetMethods().First().ReturnParameter, context)
    {
      myParameterInfo = parameterInfo;
      myContext = context;
      myCache = cache;
    }

    [ContractAnnotation("parameter:null => null")]
    public static ProxyProvidedParameterInfoWithCache CreateNoContext(RdProvidedParameterInfo parameter,
      RdFSharpTypeProvidersLoaderModel processModel, ITypeProviderCache cache) =>
      parameter == null
        ? null
        : new ProxyProvidedParameterInfoWithCache(parameter, ProvidedTypeContext.Empty, cache);

    [ContractAnnotation("parameter:null => null")]
    public static ProxyProvidedParameterInfoWithCache Create(RdProvidedParameterInfo parameter,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, ITypeProviderCache cache) =>
      parameter == null ? null : new ProxyProvidedParameterInfoWithCache(parameter, context, cache);

    public override string Name => myParameterInfo.Name;
    public override bool IsIn => myParameterInfo.IsIn;
    public override bool IsOptional => myParameterInfo.IsOptional;
    public override bool IsOut => myParameterInfo.IsOut;
    public override bool HasDefaultValue => myParameterInfo.HasDefaultValue;

    public override ProvidedType ParameterType =>
      myCache.GetOrCreateWithContext(myParameterInfo.ParameterType.Sync(Unit.Instance), myContext);
  }
}
