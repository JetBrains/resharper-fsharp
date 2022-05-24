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
  public class ProxyProvidedConstructorInfoWithContext : ProvidedConstructorInfo, IRdProvidedCustomAttributesOwner
  {
    private readonly ProvidedConstructorInfo myConstructorInfo;

    private ProxyProvidedConstructorInfoWithContext(ProvidedConstructorInfo methodInfo, ProvidedTypeContext context) :
      base(null, context)
    {
      myConstructorInfo = methodInfo;
    }

    [ContractAnnotation("methodInfo:null => null")]
    private static ProxyProvidedConstructorInfoWithContext Create(ProvidedConstructorInfo methodInfo,
      ProvidedTypeContext context)
      => methodInfo == null ? null : new ProxyProvidedConstructorInfoWithContext(methodInfo, context);

    public static ProxyProvidedConstructorInfoWithContext[] Create(ProvidedConstructorInfo[] methodInfos,
      ProvidedTypeContext context)
    {
      var result = new ProxyProvidedConstructorInfoWithContext[methodInfos.Length];
      for (var i = 0; i < methodInfos.Length; i++)
        result[i] = new ProxyProvidedConstructorInfoWithContext(methodInfos[i], context);

      return result;
    }

    public override string Name => myConstructorInfo.Name;
    public override bool IsAbstract => myConstructorInfo.IsAbstract;
    public override bool IsConstructor => myConstructorInfo.IsConstructor;
    public override bool IsFamily => myConstructorInfo.IsFamily;
    public override bool IsFinal => myConstructorInfo.IsFinal;
    public override bool IsPublic => myConstructorInfo.IsPublic;
    public override bool IsStatic => myConstructorInfo.IsStatic;
    public override bool IsVirtual => myConstructorInfo.IsVirtual;
    public override bool IsGenericMethod => myConstructorInfo.IsGenericMethod;
    public override bool IsFamilyAndAssembly => myConstructorInfo.IsFamilyAndAssembly;
    public override bool IsFamilyOrAssembly => myConstructorInfo.IsFamilyOrAssembly;
    public override bool IsHideBySig => myConstructorInfo.IsHideBySig;

    public override ProvidedType DeclaringType =>
      ProxyProvidedTypeWithContext.Create(myConstructorInfo.DeclaringType, Context);

    public override ProvidedParameterInfo[] GetStaticParametersForMethod(ITypeProvider provider) =>
      ProxyProvidedParameterInfoWithContext.Create(myConstructorInfo.GetStaticParametersForMethod(provider), Context);

    public override ProvidedMethodBase ApplyStaticArgumentsForMethod(ITypeProvider provider,
      string fullNameAfterArguments, object[] staticArgs) =>
      Create(myConstructorInfo.ApplyStaticArgumentsForMethod(provider, fullNameAfterArguments, staticArgs)
        as ProvidedConstructorInfo, Context);

    public override ProvidedParameterInfo[] GetParameters() =>
      ProxyProvidedParameterInfoWithContext.Create(myConstructorInfo.GetParameters(), Context);

    public override ProvidedType[] GetGenericArguments() =>
      ProxyProvidedTypeWithContext.Create(myConstructorInfo.GetGenericArguments(), Context);

    public override FSharpOption<
        Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider tp, string attribName) =>
      myConstructorInfo.GetAttributeConstructorArgs(tp, attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider tp) =>
      myConstructorInfo.GetDefinitionLocationAttribute(tp);

    public override string[] GetXmlDocAttributes(ITypeProvider tp) => myConstructorInfo.GetXmlDocAttributes(tp);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider tp) =>
      myConstructorInfo.GetHasTypeProviderEditorHideMethodsAttribute(tp);

    public RdCustomAttributeData[] Attributes => myConstructorInfo.GetRdCustomAttributes();
  }
}
