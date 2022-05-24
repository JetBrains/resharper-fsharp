using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public class ProxyProvidedEventInfoWithContext : ProvidedEventInfo, IRdProvidedCustomAttributesOwner
  {
    private readonly ProvidedEventInfo myEventInfo;
    private readonly ProvidedTypeContext myContext;

    private ProxyProvidedEventInfoWithContext(ProvidedEventInfo eventInfo, ProvidedTypeContext context) :
      base(null, context)
    {
      myEventInfo = eventInfo;
      myContext = context;
    }

    [ContractAnnotation("eventInfo:null => null")]
    public static ProxyProvidedEventInfoWithContext Create(ProvidedEventInfo eventInfo, ProvidedTypeContext context)
      => eventInfo == null ? null : new ProxyProvidedEventInfoWithContext(eventInfo, context);

    public static ProxyProvidedEventInfoWithContext[] Create(ProvidedEventInfo[] eventInfos,
      ProvidedTypeContext context)
    {
      var result = new ProxyProvidedEventInfoWithContext[eventInfos.Length];
      for (var i = 0; i < eventInfos.Length; i++)
        result[i] = new ProxyProvidedEventInfoWithContext(eventInfos[i], context);

      return result;
    }

    public override string Name => myEventInfo.Name;

    public override ProvidedType DeclaringType =>
      ProxyProvidedTypeWithContext.Create(myEventInfo.DeclaringType, myContext);

    public override ProvidedType EventHandlerType =>
      ProxyProvidedTypeWithContext.Create(myEventInfo.EventHandlerType, myContext);

    public override ProvidedMethodInfo GetAddMethod() =>
      ProxyProvidedMethodInfoWithContext.Create(myEventInfo.GetAddMethod(), myContext);

    public override ProvidedMethodInfo GetRemoveMethod() =>
      ProxyProvidedMethodInfoWithContext.Create(myEventInfo.GetRemoveMethod(), myContext);

    public override FSharpOption<
        Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider tp, string attribName) =>
      myEventInfo.GetAttributeConstructorArgs(tp, attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider tp) =>
      myEventInfo.GetDefinitionLocationAttribute(tp);

    public override string[] GetXmlDocAttributes(ITypeProvider tp) => myEventInfo.GetXmlDocAttributes(tp);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider tp) =>
      myEventInfo.GetHasTypeProviderEditorHideMethodsAttribute(tp);

    public RdCustomAttributeData[] Attributes => myEventInfo.GetRdCustomAttributes();
  }
}
