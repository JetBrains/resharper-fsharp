using System.Linq;
using JetBrains.Annotations;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedPropertyInfo : ProvidedPropertyInfo
  {
    private readonly RdProvidedPropertyInfo myPropertyInfo;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ProvidedTypeContext myContext;
    private int EntityId => myPropertyInfo.EntityId;

    private RdProvidedPropertyInfoProcessModel RdProvidedPropertyInfoProcessModel =>
      myProcessModel.RdProvidedPropertyInfoProcessModel;

    private ProxyProvidedPropertyInfo(RdProvidedPropertyInfo propertyInfo,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context) : base(
      typeof(string).GetProperties().First(), context)
    {
      myPropertyInfo = propertyInfo;
      myProcessModel = processModel;
      myContext = context;
    }

    [ContractAnnotation("propertyInfo:null => null")]
    public static ProxyProvidedPropertyInfo CreateNoContext(
      RdProvidedPropertyInfo propertyInfo,
      RdFSharpTypeProvidersLoaderModel processModel) =>
      propertyInfo == null
        ? null
        : new ProxyProvidedPropertyInfo(propertyInfo, processModel, ProvidedTypeContext.Empty);

    [ContractAnnotation("propertyInfo:null => null")]
    public static ProxyProvidedPropertyInfo Create(
      RdProvidedPropertyInfo propertyInfo,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context) =>
      propertyInfo == null ? null : new ProxyProvidedPropertyInfo(propertyInfo, processModel, context);

    public override string Name => myPropertyInfo.Name;
    public override bool CanRead => myPropertyInfo.CanRead;
    public override bool CanWrite => myPropertyInfo.CanWrite;

    public override ProvidedType DeclaringType =>
      ProxyProvidedType.Create(
        RdProvidedPropertyInfoProcessModel.DeclaringType.Sync(EntityId),
        myProcessModel,
        myContext);

    public override ProvidedType PropertyType =>
      ProxyProvidedType.Create(
        RdProvidedPropertyInfoProcessModel.PropertyType.Sync(EntityId),
        myProcessModel,
        myContext);

    public override ProvidedMethodInfo GetGetMethod() =>
      ProxyProvidedMethodInfo.Create(
        RdProvidedPropertyInfoProcessModel.GetGetMethod.Sync(EntityId),
        myProcessModel,
        myContext);

    public override ProvidedMethodInfo GetSetMethod() =>
      ProxyProvidedMethodInfo.Create(
        RdProvidedPropertyInfoProcessModel.GetSetMethod.Sync(EntityId),
        myProcessModel,
        myContext);

    public override ProvidedParameterInfo[] GetIndexParameters() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedPropertyInfoProcessModel.GetIndexParameters
        .Sync(EntityId)
        .Select(t => ProxyProvidedParameterInfo.Create(t, myProcessModel, myContext))
        .ToArray();
  }
}
