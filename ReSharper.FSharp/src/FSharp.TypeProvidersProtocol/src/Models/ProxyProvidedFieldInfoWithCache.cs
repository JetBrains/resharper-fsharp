using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using JetBrains.Util.Concurrency;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedFieldInfoWithCache : ProvidedFieldInfo
  {
    private readonly RdProvidedFieldInfo myFieldInfo;
    private readonly int myTypeProviderId;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly IProvidedTypesCache myCache;
    private readonly ProvidedTypeContext myContext;

    private int EntityId => myFieldInfo.EntityId;

    private RdProvidedFieldInfoProcessModel RdProvidedFieldInfoProcessModel =>
      myProcessModel.RdProvidedFieldInfoProcessModel;

    private ProxyProvidedFieldInfoWithCache(RdProvidedFieldInfo fieldInfo, int typeProviderId,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context, IProvidedTypesCache cache) : base(typeof(string).GetFields().First(), context)
    {
      myFieldInfo = fieldInfo;
      myTypeProviderId = typeProviderId;
      myProcessModel = processModel;
      myCache = cache;
      myContext = context;

      myRawConstantValue = new InterruptibleLazy<object>(() =>
        RdProvidedFieldInfoProcessModel.GetRawConstantValue.Sync(EntityId).Unbox());
    }

    [ContractAnnotation("fieldInfo:null => null")]
    public static ProxyProvidedFieldInfoWithCache Create(RdProvidedFieldInfo fieldInfo, int typeProviderId,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context, IProvidedTypesCache cache) =>
      fieldInfo == null ? null : new ProxyProvidedFieldInfoWithCache(fieldInfo, typeProviderId, processModel, context, cache);

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

    public override ProvidedType DeclaringType => myCache.GetOrCreateWithContext(
      myDeclaringTypeId ??= RdProvidedFieldInfoProcessModel.DeclaringType.Sync(EntityId), myTypeProviderId, myContext);

    public override ProvidedType FieldType => myCache.GetOrCreateWithContext(
      myFieldTypeId ??= RdProvidedFieldInfoProcessModel.FieldType.Sync(EntityId), myTypeProviderId, myContext);

    public override object GetRawConstantValue() => myRawConstantValue.Value;

    private bool HasFlag(RdProvidedFieldFlags flag) => (myFieldInfo.Flags & flag) == flag;

    private int? myDeclaringTypeId;
    private int? myFieldTypeId;
    private readonly InterruptibleLazy<object> myRawConstantValue;
  }
}
