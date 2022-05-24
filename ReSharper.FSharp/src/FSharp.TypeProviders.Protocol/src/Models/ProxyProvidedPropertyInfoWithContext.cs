using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
  public class ProxyProvidedPropertyInfoWithContext : ProvidedPropertyInfo, IRdProvidedCustomAttributesOwner
  {
    private readonly ProvidedPropertyInfo myPropertyInfo;
    private readonly ProvidedTypeContext myContext;

    private ProxyProvidedPropertyInfoWithContext(ProvidedPropertyInfo propertyInfo, ProvidedTypeContext context) :
      base(null, context)
    {
      myPropertyInfo = propertyInfo;
      myContext = context;
    }

    [ContractAnnotation("propertyInfo:null => null")]
    public static ProxyProvidedPropertyInfoWithContext Create(ProvidedPropertyInfo propertyInfo,
      ProvidedTypeContext context) =>
      propertyInfo == null ? null : new ProxyProvidedPropertyInfoWithContext(propertyInfo, context);

    public static ProxyProvidedPropertyInfoWithContext[] Create(ProvidedPropertyInfo[] propertyInfos,
      ProvidedTypeContext context)
    {
      var result = new ProxyProvidedPropertyInfoWithContext[propertyInfos.Length];
      for (var i = 0; i < propertyInfos.Length; i++)
        result[i] = new ProxyProvidedPropertyInfoWithContext(propertyInfos[i], context);

      return result;
    }

    public override string Name => myPropertyInfo.Name;
    public override bool CanRead => myPropertyInfo.CanRead;
    public override bool CanWrite => myPropertyInfo.CanWrite;

    public override ProvidedType DeclaringType =>
      ProxyProvidedTypeWithContext.Create(myPropertyInfo.DeclaringType, myContext);

    public override ProvidedType PropertyType =>
      ProxyProvidedTypeWithContext.Create(myPropertyInfo.PropertyType, myContext);

    public override ProvidedMethodInfo GetGetMethod() =>
      ProxyProvidedMethodInfoWithContext.Create(myPropertyInfo.GetGetMethod(), myContext);

    public override ProvidedMethodInfo GetSetMethod() =>
      ProxyProvidedMethodInfoWithContext.Create(myPropertyInfo.GetSetMethod(), myContext);

    public override ProvidedParameterInfo[] GetIndexParameters() =>
      ProxyProvidedParameterInfoWithContext.Create(myPropertyInfo.GetIndexParameters(), myContext);

    public override FSharpOption<
        Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider tp, string attribName) =>
      myPropertyInfo.GetAttributeConstructorArgs(tp, attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider tp) =>
      myPropertyInfo.GetDefinitionLocationAttribute(tp);

    public override string[] GetXmlDocAttributes(ITypeProvider tp) =>
      myPropertyInfo.GetXmlDocAttributes(tp);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider tp) =>
      myPropertyInfo.GetHasTypeProviderEditorHideMethodsAttribute(tp);

    public RdCustomAttributeData[] Attributes => myPropertyInfo.GetRdCustomAttributes();
  }
}
