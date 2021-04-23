using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public class ProxyProvidedParameterInfo : ProvidedParameterInfo
  {
    private readonly RdProvidedParameterInfo myParameterInfo;
    private readonly int myTypeProviderId;
    private readonly TypeProvidersContext myTypeProvidersContext;
    private readonly ProvidedTypeContextHolder myContext;

    private ProxyProvidedParameterInfo(RdProvidedParameterInfo parameterInfo, int typeProviderId,
      TypeProvidersContext typeProvidersContext, ProvidedTypeContextHolder context) : base(null, context.Context)
    {
      myParameterInfo = parameterInfo;
      myTypeProviderId = typeProviderId;
      myTypeProvidersContext = typeProvidersContext;
      myContext = context;
      RawDefaultValue = myParameterInfo.RawDefaultValue.Unbox();
    }

    [ContractAnnotation("parameter:null => null")]
    public static ProxyProvidedParameterInfo Create(RdProvidedParameterInfo parameter, int typeProviderId,
      TypeProvidersContext typeProvidersContext, ProvidedTypeContextHolder context) =>
      parameter == null
        ? null
        : new ProxyProvidedParameterInfo(parameter, typeProviderId, typeProvidersContext, context);

    public override string Name => myParameterInfo.Name;
    public override bool IsIn => myParameterInfo.IsIn;
    public override bool IsOptional => myParameterInfo.IsOptional;
    public override bool IsOut => myParameterInfo.IsOut;
    public override object RawDefaultValue { get; }
    public override bool HasDefaultValue => myParameterInfo.HasDefaultValue;

    public override
      FSharpOption<Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider _, string attribName) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetAttributeConstructorArgs(
        myParameterInfo.CustomAttributes,
        attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetDefinitionLocationAttribute(myParameterInfo
        .CustomAttributes);

    public override string[] GetXmlDocAttributes(ITypeProvider _) =>
      myXmlDocs ??=
        myTypeProvidersContext.ProvidedCustomAttributeProvider.GetXmlDocAttributes(myParameterInfo.CustomAttributes);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetHasTypeProviderEditorHideMethodsAttribute(
        myParameterInfo
          .CustomAttributes);

    public override ProvidedType ParameterType =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(myParameterInfo.ParameterType, myTypeProviderId, myContext);

    private string[] myXmlDocs;
  }
}
