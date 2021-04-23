using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using JetBrains.Util.Concurrency;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public class ProxyProvidedPropertyInfo : ProvidedPropertyInfo, IRdProvidedEntity
  {
    private readonly RdProvidedPropertyInfo myPropertyInfo;
    private readonly int myTypeProviderId;
    private readonly TypeProvidersContext myTypeProvidersContext;
    private readonly ProvidedTypeContextHolder myContext;
    public int EntityId => myPropertyInfo.EntityId;
    public RdProvidedEntityType EntityType => RdProvidedEntityType.PropertyInfo;

    private ProxyProvidedPropertyInfo(RdProvidedPropertyInfo propertyInfo, int typeProviderId,
      TypeProvidersContext typeProvidersContext, ProvidedTypeContextHolder context) : base(null, context.Context)
    {
      myPropertyInfo = propertyInfo;
      myTypeProviderId = typeProviderId;
      myTypeProvidersContext = typeProvidersContext;
      myContext = context;

      myMethods = new InterruptibleLazy<ProvidedMethodInfo[]>(() =>
        // ReSharper disable once CoVariantArrayConversion
        GetMethodsInfos()
          .Select(t => ProxyProvidedMethodInfo.Create(t, typeProviderId, typeProvidersContext, context))
          .ToArray());

      myIndexParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(
        () => // ReSharper disable once CoVariantArrayConversion
          propertyInfo.IndexParameters
            .Select(t => ProxyProvidedParameterInfo.Create(t, typeProviderId, typeProvidersContext, context))
            .ToArray());

      myCustomAttributes = new InterruptibleLazy<RdCustomAttributeData[]>(() =>
        myTypeProvidersContext.ProvidedCustomAttributeProvider.GetCustomAttributes(this));
    }

    [ContractAnnotation("propertyInfo:null => null")]
    public static ProxyProvidedPropertyInfo Create(RdProvidedPropertyInfo propertyInfo, int typeProviderId,
      TypeProvidersContext typeProvidersContext, ProvidedTypeContextHolder context) =>
      propertyInfo == null
        ? null
        : new ProxyProvidedPropertyInfo(propertyInfo, typeProviderId, typeProvidersContext, context);

    public override string Name => myPropertyInfo.Name;
    public override bool CanRead => myPropertyInfo.CanRead;
    public override bool CanWrite => myPropertyInfo.CanWrite;

    public override ProvidedType DeclaringType =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(myPropertyInfo.DeclaringType, myTypeProviderId, myContext);

    public override ProvidedType PropertyType =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(myPropertyInfo.PropertyType, myTypeProviderId, myContext);

    public override ProvidedMethodInfo GetGetMethod() => myMethods.Value[0];

    public override ProvidedMethodInfo GetSetMethod() => myMethods.Value[1];

    public override ProvidedParameterInfo[] GetIndexParameters() => myIndexParameters.Value;

    public override
      FSharpOption<Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider _, string attribName) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetAttributeConstructorArgs(myCustomAttributes.Value,
        attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetDefinitionLocationAttribute(myCustomAttributes.Value);

    public override string[] GetXmlDocAttributes(ITypeProvider _) =>
      myXmlDocs ??=
        myTypeProvidersContext.ProvidedCustomAttributeProvider.GetXmlDocAttributes(myCustomAttributes.Value);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetHasTypeProviderEditorHideMethodsAttribute(
        myCustomAttributes.Value);

    private RdProvidedMethodInfo[] GetMethodsInfos()
    {
      if (myPropertyInfo.GetMethod != 0 && myPropertyInfo.SetMethod != 0)
        return myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
          myTypeProvidersContext.Connection.ProtocolModel.RdProvidedMethodInfoProcessModel.GetProvidedMethodInfos
            .Sync(new[] {myPropertyInfo.GetMethod, myPropertyInfo.SetMethod}, RpcTimeouts.Maximal));

      var infos = new RdProvidedMethodInfo[2];

      if (myPropertyInfo.GetMethod != 0)
        infos[0] = myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
          myTypeProvidersContext.Connection.ProtocolModel.RdProvidedMethodInfoProcessModel.GetProvidedMethodInfo
            .Sync(myPropertyInfo.GetMethod, RpcTimeouts.Maximal));

      else if (myPropertyInfo.SetMethod != 0)
        infos[1] = myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
          myTypeProvidersContext.Connection.ProtocolModel.RdProvidedMethodInfoProcessModel.GetProvidedMethodInfo
            .Sync(myPropertyInfo.SetMethod, RpcTimeouts.Maximal));

      return infos;
    }

    private string[] myXmlDocs;
    private readonly InterruptibleLazy<ProvidedMethodInfo[]> myMethods;
    private readonly InterruptibleLazy<ProvidedParameterInfo[]> myIndexParameters;
    private readonly InterruptibleLazy<RdCustomAttributeData[]> myCustomAttributes;
  }
}
