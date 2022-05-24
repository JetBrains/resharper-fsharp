using System;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using JetBrains.Util;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public class ProxyProvidedParameterInfoWithContext : ProvidedParameterInfo, IRdProvidedCustomAttributesOwner
  {
    private readonly ProvidedParameterInfo myParameterInfo;
    private readonly ProvidedTypeContext myContext;

    private ProxyProvidedParameterInfoWithContext(ProvidedParameterInfo parameterInfo, ProvidedTypeContext context)
      : base(null, context)
    {
      myParameterInfo = parameterInfo;
      myContext = context;
    }

    public static ProxyProvidedParameterInfoWithContext[] Create(ProvidedParameterInfo[] parameterInfos,
      ProvidedTypeContext context)
    {
      var result = new ProxyProvidedParameterInfoWithContext[parameterInfos.Length];
      for (var i = 0; i < parameterInfos.Length; i++)
        result[i] = new ProxyProvidedParameterInfoWithContext(parameterInfos[i], context);

      return result;
    }

    public override string Name => myParameterInfo.Name;
    public override bool IsIn => myParameterInfo.IsIn;
    public override bool IsOptional => myParameterInfo.IsOptional;
    public override bool IsOut => myParameterInfo.IsOut;
    public override object RawDefaultValue => myParameterInfo.RawDefaultValue;
    public override bool HasDefaultValue => myParameterInfo.HasDefaultValue;

    public override ProvidedType ParameterType =>
      ProxyProvidedTypeWithContext.Create(myParameterInfo.ParameterType, myContext);

    public override FSharpOption<
        Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider tp, string attribName) =>
      myParameterInfo.GetAttributeConstructorArgs(tp, attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider tp) =>
      myParameterInfo.GetDefinitionLocationAttribute(tp);

    public override string[] GetXmlDocAttributes(ITypeProvider tp) =>
      myParameterInfo.GetXmlDocAttributes(tp);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider tp) =>
      myParameterInfo.GetHasTypeProviderEditorHideMethodsAttribute(tp);

    public RdCustomAttributeData[] Attributes =>
      myParameterInfo is IRdProvidedCustomAttributesOwner x
        ? x.Attributes
        : EmptyArray<RdCustomAttributeData>.Instance;
  }
}
