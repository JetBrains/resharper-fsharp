using System;
using System.Diagnostics.CodeAnalysis;
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
  [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
  public class ProxyProvidedEventInfo : ProvidedEventInfo, IRdProvidedCustomAttributesOwner
  {
    private readonly RdProvidedEventInfo myEventInfo;
    private readonly TypeProvidersContext myTypeProvidersContext;

    private ProxyProvidedEventInfo(RdProvidedEventInfo eventInfo, IProxyTypeProvider typeProvider,
      TypeProvidersContext typeProvidersContext) : base(null, ProvidedConst.EmptyContext)
    {
      myEventInfo = eventInfo;
      myTypeProvidersContext = typeProvidersContext;

      myMethods = new InterruptibleLazy<ProvidedMethodInfo[]>(() =>
        myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
          typeProvidersContext.Connection.ProtocolModel.RdProvidedMethodInfoProcessModel.GetProvidedMethodInfos
            .Sync(new[] { eventInfo.AddMethod, eventInfo.RemoveMethod }, RpcTimeouts.Maximal)
            .Select(t => ProxyProvidedMethodInfo.Create(t, typeProvider, typeProvidersContext))
            .ToArray()));

      myTypes = new InterruptibleLazy<ProvidedType[]>(() =>
        myTypeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(
          new[] { eventInfo.DeclaringType, eventInfo.EventHandlerType }, typeProvider));
    }

    [ContractAnnotation("eventInfo:null => null")]
    public static ProxyProvidedEventInfo Create(RdProvidedEventInfo eventInfo, IProxyTypeProvider typeProvider,
      TypeProvidersContext typeProvidersContext) =>
      eventInfo == null ? null : new ProxyProvidedEventInfo(eventInfo, typeProvider, typeProvidersContext);

    public override string Name => myEventInfo.Name;

    public override ProvidedType DeclaringType => myTypes.Value[0];

    public override ProvidedType EventHandlerType => myTypes.Value[1];

    public override ProvidedMethodInfo GetAddMethod() => myMethods.Value[0];

    public override ProvidedMethodInfo GetRemoveMethod() => myMethods.Value[1];

    public override
      FSharpOption<Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider _, string attribName) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetAttributeConstructorArgs(myEventInfo.CustomAttributes,
        attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetDefinitionLocationAttribute(
        myEventInfo.CustomAttributes);

    public override string[] GetXmlDocAttributes(ITypeProvider _) =>
      myXmlDocs ??=
        myTypeProvidersContext.ProvidedCustomAttributeProvider.GetXmlDocAttributes(myEventInfo.CustomAttributes);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetHasTypeProviderEditorHideMethodsAttribute(myEventInfo
        .CustomAttributes);

    private string[] myXmlDocs;
    private readonly InterruptibleLazy<ProvidedMethodInfo[]> myMethods;
    private readonly InterruptibleLazy<ProvidedType[]> myTypes;
    public RdCustomAttributeData[] Attributes => myEventInfo.CustomAttributes;
  }
}
