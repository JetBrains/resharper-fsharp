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
  public class ProxyProvidedFieldInfoWithContext : ProvidedFieldInfo, IRdProvidedCustomAttributesOwner
  {
    private readonly ProvidedFieldInfo myFieldInfo;
    private readonly ProvidedTypeContext myContext;

    private ProxyProvidedFieldInfoWithContext(ProvidedFieldInfo fieldInfo, ProvidedTypeContext context) :
      base(null, context)
    {
      myFieldInfo = fieldInfo;
      myContext = context;
    }

    [ContractAnnotation("fieldInfo:null => null")]
    public static ProxyProvidedFieldInfoWithContext Create(ProvidedFieldInfo fieldInfo, ProvidedTypeContext context) =>
      fieldInfo == null ? null : new ProxyProvidedFieldInfoWithContext(fieldInfo, context);

    public static ProxyProvidedFieldInfoWithContext[] Create(ProvidedFieldInfo[] fieldInfos,
      ProvidedTypeContext context)
    {
      var result = new ProxyProvidedFieldInfoWithContext[fieldInfos.Length];
      for (var i = 0; i < fieldInfos.Length; i++)
        result[i] = new ProxyProvidedFieldInfoWithContext(fieldInfos[i], context);

      return result;
    }

    public override string Name => myFieldInfo.Name;
    public override bool IsFamily => myFieldInfo.IsFamily;
    public override bool IsLiteral => myFieldInfo.IsLiteral;
    public override bool IsPrivate => myFieldInfo.IsPrivate;
    public override bool IsPublic => myFieldInfo.IsPublic;
    public override bool IsStatic => myFieldInfo.IsStatic;
    public override bool IsInitOnly => myFieldInfo.IsInitOnly;
    public override bool IsSpecialName => myFieldInfo.IsSpecialName;
    public override bool IsFamilyAndAssembly => myFieldInfo.IsFamilyAndAssembly;
    public override bool IsFamilyOrAssembly => myFieldInfo.IsFamilyOrAssembly;

    public override ProvidedType DeclaringType =>
      ProxyProvidedTypeWithContext.Create(myFieldInfo.DeclaringType, myContext);

    public override ProvidedType FieldType =>
      ProxyProvidedTypeWithContext.Create(myFieldInfo.FieldType, myContext);

    public override object GetRawConstantValue() => myFieldInfo.GetRawConstantValue();

    public override FSharpOption<
        Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider tp, string attribName) =>
      myFieldInfo.GetAttributeConstructorArgs(tp, attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider tp) =>
      myFieldInfo.GetDefinitionLocationAttribute(tp);

    public override string[] GetXmlDocAttributes(ITypeProvider tp) => myFieldInfo.GetXmlDocAttributes(tp);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider tp) =>
      myFieldInfo.GetHasTypeProviderEditorHideMethodsAttribute(tp);

    public RdCustomAttributeData[] Attributes => myFieldInfo.GetRdCustomAttributes();
  }
}
