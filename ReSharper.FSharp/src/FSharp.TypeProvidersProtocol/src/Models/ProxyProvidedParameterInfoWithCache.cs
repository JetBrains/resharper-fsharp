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
    private readonly int myTypeProviderId;
    private readonly ProvidedTypeContext myContext;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly IProvidedTypesCache myCache;

    private RdProvidedParameterInfoProcessModel RdProvidedParameterInfoProcessModel =>
      myProcessModel.RdProvidedParameterInfoProcessModel;

    private int EntityId => myParameterInfo.EntityId;

    private ProxyProvidedParameterInfoWithCache(RdProvidedParameterInfo parameterInfo, int typeProviderId,
      ProvidedTypeContext context,
      RdFSharpTypeProvidersLoaderModel processModel, IProvidedTypesCache cache) : base(
      typeof(string).GetMethods().First().ReturnParameter, context)
    {
      myParameterInfo = parameterInfo;
      myTypeProviderId = typeProviderId;
      myContext = context;
      myProcessModel = processModel;
      myCache = cache;
    }

    [ContractAnnotation("parameter:null => null")]
    public static ProxyProvidedParameterInfoWithCache Create(RdProvidedParameterInfo parameter, int typeProviderId,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, IProvidedTypesCache cache) =>
      parameter == null
        ? null
        : new ProxyProvidedParameterInfoWithCache(parameter, typeProviderId, context, processModel, cache);

    public override string Name => myParameterInfo.Name;
    public override bool IsIn => myParameterInfo.IsIn;
    public override bool IsOptional => myParameterInfo.IsOptional;
    public override bool IsOut => myParameterInfo.IsOut;
    public override object RawDefaultValue => myParameterInfo.RawDefaultValue.Unbox(); //TODO: cache?
    public override bool HasDefaultValue => myParameterInfo.HasDefaultValue;

    public override ProvidedType ParameterType =>
      myCache.GetOrCreateWithContext(
        myParameterTypeId ??= RdProvidedParameterInfoProcessModel.ParameterType.Sync(EntityId), myTypeProviderId,
        myContext);

    private int? myParameterTypeId;
  }
}
