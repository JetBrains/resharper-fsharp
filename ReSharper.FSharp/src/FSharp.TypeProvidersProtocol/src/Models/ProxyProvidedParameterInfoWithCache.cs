using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedParameterInfoWithCache : ProvidedParameterInfo
  {
    private readonly RdProvidedParameterInfo myParameterInfo;
    private readonly ProvidedTypeContext myContext;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ITypeProviderCache myCache;

    private RdProvidedParameterInfoProcessModel RdProvidedParameterInfoProcessModel =>
      myProcessModel.RdProvidedParameterInfoProcessModel;

    private int EntityId => myParameterInfo.EntityId;

    public ProxyProvidedParameterInfoWithCache(RdProvidedParameterInfo parameterInfo, ProvidedTypeContext context,
      RdFSharpTypeProvidersLoaderModel processModel, ITypeProviderCache cache) : base(
      typeof(string).GetMethods().First().ReturnParameter, context)
    {
      myParameterInfo = parameterInfo;
      myContext = context;
      myProcessModel = processModel;
      myCache = cache;
    }

    [ContractAnnotation("parameter:null => null")]
    public static ProxyProvidedParameterInfoWithCache Create(RdProvidedParameterInfo parameter,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, ITypeProviderCache cache) =>
      parameter == null ? null : new ProxyProvidedParameterInfoWithCache(parameter, context, processModel, cache);

    public override string Name => myParameterInfo.Name;
    public override bool IsIn => myParameterInfo.IsIn;
    public override bool IsOptional => myParameterInfo.IsOptional;
    public override bool IsOut => myParameterInfo.IsOut;
    public override object RawDefaultValue => myParameterInfo.RawDefaultValue.Unbox(); //TODO: cache?
    public override bool HasDefaultValue => myParameterInfo.HasDefaultValue;

    public override ProvidedType ParameterType =>
      myCache.GetOrCreateWithContext(
        myParameterTypeId ??= RdProvidedParameterInfoProcessModel.ParameterType.Sync(EntityId), myContext);

    private int? myParameterTypeId;
  }
}
