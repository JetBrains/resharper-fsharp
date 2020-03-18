using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedFieldInfoWithCache : ProvidedFieldInfo
  {
    private readonly RdProvidedFieldInfo myFieldInfo;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ITypeProviderCache myCache;
    private readonly ProvidedTypeContext myContext;

    private int EntityId => myFieldInfo.EntityId;

    private RdProvidedFieldInfoProcessModel RdProvidedFieldInfoProcessModel =>
      myProcessModel.RdProvidedFieldInfoProcessModel;

    public ProxyProvidedFieldInfoWithCache(RdProvidedFieldInfo fieldInfo, RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context, ITypeProviderCache cache) : base(typeof(string).GetFields().First(), context)
    {
      myFieldInfo = fieldInfo;
      myProcessModel = processModel;
      myCache = cache;
      myContext = context;

      myRawConstantValue = new Lazy<object>(() =>
        RdProvidedFieldInfoProcessModel.GetRawConstantValue.Sync(EntityId).Unbox());
    }

    [ContractAnnotation("fieldInfo:null => null")]
    public static ProxyProvidedFieldInfoWithCache CreateWithContext(RdProvidedFieldInfo fieldInfo,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, ITypeProviderCache cache) =>
      fieldInfo == null ? null : new ProxyProvidedFieldInfoWithCache(fieldInfo, processModel, context, cache);

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

    public override ProvidedType DeclaringType => myCache.GetOrCreateWithContext(
      myDeclaringTypeId ??= RdProvidedFieldInfoProcessModel.DeclaringType.Sync(EntityId), myContext);

    public override ProvidedType FieldType => myCache.GetOrCreateWithContext(
      myFieldTypeId ??= RdProvidedFieldInfoProcessModel.FieldType.Sync(EntityId), myContext);

    public override object GetRawConstantValue() => myRawConstantValue.Value;

    private int? myDeclaringTypeId;
    private int? myFieldTypeId;
    private readonly Lazy<object> myRawConstantValue;
  }
}
