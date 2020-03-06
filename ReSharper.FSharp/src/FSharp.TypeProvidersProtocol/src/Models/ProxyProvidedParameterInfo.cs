using System.Linq;
using JetBrains.Annotations;
using JetBrains.Core;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedParameterInfo : ProvidedParameterInfo
  {
    private readonly RdProvidedParameterInfo myParameterInfo;
    private readonly ProvidedTypeContext myContext;
    private readonly ITypeProviderCache myCache;

    public ProxyProvidedParameterInfo(RdProvidedParameterInfo parameterInfo, ProvidedTypeContext context,
      ITypeProviderCache cache) : base(
      typeof(string).GetMethods().First().ReturnParameter, context)
    {
      myParameterInfo = parameterInfo;
      myContext = context;
      myCache = cache;
    }

    [ContractAnnotation("parameter:null => null")]
    public static ProxyProvidedParameterInfo CreateNoContext(RdProvidedParameterInfo parameter,
      RdFSharpTypeProvidersLoaderModel processModel, ITypeProviderCache cache) =>
      parameter == null
        ? null
        : new ProxyProvidedParameterInfo(parameter, ProvidedTypeContext.Empty, cache);

    [ContractAnnotation("parameter:null => null")]
    public static ProxyProvidedParameterInfo Create(RdProvidedParameterInfo parameter,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, ITypeProviderCache cache) =>
      parameter == null ? null : new ProxyProvidedParameterInfo(parameter, context, cache);

    public override string Name => myParameterInfo.Name;
    public override bool IsIn => myParameterInfo.IsIn;
    public override bool IsOptional => myParameterInfo.IsOptional;
    public override bool IsOut => myParameterInfo.IsOut;
    public override bool HasDefaultValue => myParameterInfo.HasDefaultValue;

    public override ProvidedType ParameterType =>
      myCache.GetOrCreateWithContextProvidedType(myParameterInfo.ParameterType.Sync(Unit.Instance), myContext);
  }
}
