using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using JetBrains.Util.Concurrency;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedFieldInfo : ProvidedFieldInfo
  {
    private readonly RdProvidedFieldInfo myFieldInfo;
    private readonly TypeProvidersContext myTypeProvidersContext;

    private ProxyProvidedFieldInfo(RdProvidedFieldInfo fieldInfo, int typeProviderId,
      TypeProvidersContext typeProvidersContext, ProvidedTypeContextHolder context) : base(null, context.Context)
    {
      myFieldInfo = fieldInfo;
      myTypeProvidersContext = typeProvidersContext;
      myRawConstantValue = myFieldInfo.RawConstantValue.Unbox();

      myTypes = new InterruptibleLazy<ProvidedType[]>(() =>
        myTypeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(new[] {fieldInfo.DeclaringType, fieldInfo.FieldType},
          typeProviderId, context));
    }

    [ContractAnnotation("fieldInfo:null => null")]
    public static ProxyProvidedFieldInfo Create(RdProvidedFieldInfo fieldInfo, int typeProviderId,
      TypeProvidersContext typeProvidersContext, ProvidedTypeContextHolder context) =>
      fieldInfo == null
        ? null
        : new ProxyProvidedFieldInfo(fieldInfo, typeProviderId, typeProvidersContext, context);

    public override string Name => myFieldInfo.Name;
    public override bool IsFamily => HasFlag(RdProvidedFieldFlags.IsFamily);
    public override bool IsLiteral => HasFlag(RdProvidedFieldFlags.IsLiteral);
    public override bool IsPrivate => HasFlag(RdProvidedFieldFlags.IsPrivate);
    public override bool IsPublic => HasFlag(RdProvidedFieldFlags.IsPublic);
    public override bool IsStatic => HasFlag(RdProvidedFieldFlags.IsStatic);
    public override bool IsInitOnly => HasFlag(RdProvidedFieldFlags.IsInitOnly);
    public override bool IsSpecialName => HasFlag(RdProvidedFieldFlags.IsSpecialName);
    public override bool IsFamilyAndAssembly => HasFlag(RdProvidedFieldFlags.IsFamilyAndAssembly);
    public override bool IsFamilyOrAssembly => HasFlag(RdProvidedFieldFlags.IsFamilyOrAssembly);

    public override ProvidedType DeclaringType => myTypes.Value[0];

    public override ProvidedType FieldType => myTypes.Value[1];

    public override object GetRawConstantValue() => myRawConstantValue;

    public override
      FSharpOption<Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider _, string attribName) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetAttributeConstructorArgs(myFieldInfo.CustomAttributes,
        attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetDefinitionLocationAttribute(
        myFieldInfo.CustomAttributes);

    public override string[] GetXmlDocAttributes(ITypeProvider _) =>
      myXmlDocs ??=
        myTypeProvidersContext.ProvidedCustomAttributeProvider.GetXmlDocAttributes(myFieldInfo.CustomAttributes);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetHasTypeProviderEditorHideMethodsAttribute(
        myFieldInfo.CustomAttributes);

    private bool HasFlag(RdProvidedFieldFlags flag) => (myFieldInfo.Flags & flag) == flag;

    private readonly InterruptibleLazy<ProvidedType[]> myTypes;
    private readonly object myRawConstantValue;
    private string[] myXmlDocs;
  }
}
