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
  public class ProxyProvidedMethodInfoWithContext : ProvidedMethodInfo, IRdProvidedCustomAttributesOwner
  {
    private readonly ProvidedMethodInfo myMethodInfo;

    private ProxyProvidedMethodInfoWithContext(ProvidedMethodInfo methodInfo, ProvidedTypeContext context) :
      base(null, context)
    {
      myMethodInfo = methodInfo;
    }

    [ContractAnnotation("methodInfo:null => null")]
    public static ProxyProvidedMethodInfoWithContext Create(ProvidedMethodInfo methodInfo,
      ProvidedTypeContext context)
      => methodInfo == null ? null : new ProxyProvidedMethodInfoWithContext(methodInfo, context);

    public static ProxyProvidedMethodInfoWithContext[] Create(ProvidedMethodInfo[] methodInfos,
      ProvidedTypeContext context)
    {
      var result = new ProxyProvidedMethodInfoWithContext[methodInfos.Length];
      for (var i = 0; i < methodInfos.Length; i++)
        result[i] = new ProxyProvidedMethodInfoWithContext(methodInfos[i], context);

      return result;
    }

    public override string Name => myMethodInfo.Name;
    public override bool IsAbstract => myMethodInfo.IsAbstract;
    public override bool IsConstructor => myMethodInfo.IsConstructor;
    public override bool IsFamily => myMethodInfo.IsFamily;
    public override bool IsFinal => myMethodInfo.IsFinal;
    public override bool IsPublic => myMethodInfo.IsPublic;
    public override bool IsStatic => myMethodInfo.IsStatic;
    public override bool IsVirtual => myMethodInfo.IsVirtual;
    public override bool IsGenericMethod => myMethodInfo.IsGenericMethod;
    public override bool IsFamilyAndAssembly => myMethodInfo.IsFamilyAndAssembly;
    public override bool IsFamilyOrAssembly => myMethodInfo.IsFamilyOrAssembly;
    public override bool IsHideBySig => myMethodInfo.IsHideBySig;
    public override int MetadataToken => myMethodInfo.MetadataToken;

    public override ProvidedType DeclaringType =>
      ProxyProvidedTypeWithContext.Create(myMethodInfo.DeclaringType, Context);

    public override ProvidedType ReturnType =>
      ProxyProvidedTypeWithContext.Create(myMethodInfo.ReturnType, Context);

    public override ProvidedParameterInfo[] GetStaticParametersForMethod(ITypeProvider provider) =>
      ProxyProvidedParameterInfoWithContext.Create(myMethodInfo.GetStaticParametersForMethod(provider), Context);

    public override ProvidedMethodBase ApplyStaticArgumentsForMethod(ITypeProvider provider,
      string fullNameAfterArguments, object[] staticArgs) =>
      Create(myMethodInfo.ApplyStaticArgumentsForMethod(provider, fullNameAfterArguments, staticArgs)
        as ProvidedMethodInfo, Context);

    public override ProvidedParameterInfo[] GetParameters() =>
      ProxyProvidedParameterInfoWithContext.Create(myMethodInfo.GetParameters(), Context);

    public override ProvidedType[] GetGenericArguments() =>
      ProxyProvidedTypeWithContext.Create(myMethodInfo.GetGenericArguments(), Context);

    public override FSharpOption<
        Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider tp, string attribName) =>
      myMethodInfo.GetAttributeConstructorArgs(tp, attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider tp) =>
      myMethodInfo.GetDefinitionLocationAttribute(tp);

    public override string[] GetXmlDocAttributes(ITypeProvider tp) => myMethodInfo.GetXmlDocAttributes(tp);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider tp) =>
      myMethodInfo.GetHasTypeProviderEditorHideMethodsAttribute(tp);

    public RdCustomAttributeData[] Attributes => myMethodInfo.GetRdCustomAttributes();
  }
}
