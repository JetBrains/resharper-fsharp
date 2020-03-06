using System.Linq;
using JetBrains.Annotations;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedMethodInfo : ProvidedMethodInfo
  {
    private readonly RdProvidedMethodInfo myMethodInfo;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ProvidedTypeContext myContext;
    private int EntityId => myMethodInfo.EntityId;

    private RdProvidedMethodInfoProcessModel RdProvidedMethodInfoProcessModel =>
      myProcessModel.RdProvidedMethodInfoProcessModel;

    private ProxyProvidedMethodInfo(
      RdProvidedMethodInfo methodInfo,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context) : base(
      typeof(string).GetMethods().First(),
      ProvidedTypeContext.Empty)
    {
      myMethodInfo = methodInfo;
      myProcessModel = processModel;
      myContext = context;
    }

    [ContractAnnotation("methodInfo:null => null")]
    public static ProxyProvidedMethodInfo CreateNoContext(
      RdProvidedMethodInfo methodInfo,
      RdFSharpTypeProvidersLoaderModel processModel) =>
      methodInfo == null ? null : new ProxyProvidedMethodInfo(methodInfo, processModel, ProvidedTypeContext.Empty);

    [ContractAnnotation("methodInfo:null => null")]
    public static ProxyProvidedMethodInfo Create(
      RdProvidedMethodInfo methodInfo,
      RdFSharpTypeProvidersLoaderModel processModel, ProvidedTypeContext context) =>
      methodInfo == null ? null : new ProxyProvidedMethodInfo(methodInfo, processModel, context);

    public override string Name => myMethodInfo.Name;
    public override bool IsAbstract => myMethodInfo.IsAbstract;
    public override bool IsConstructor => myMethodInfo.IsConstructor;
    public override bool IsFamily => myMethodInfo.IsFamily;
    public override bool IsFinal => myMethodInfo.IsFinal;
    public override bool IsPublic => myMethodInfo.IsPublic;
    public override bool IsStatic => myMethodInfo.IsStatic;
    public override bool IsVirtual => myMethodInfo.IsVirtual;
    public override int MetadataToken => myMethodInfo.MetadataToken;
    public override bool IsGenericMethod => myMethodInfo.IsGenericMethod;
    public override bool IsFamilyAndAssembly => myMethodInfo.IsFamilyAndAssembly;
    public override bool IsFamilyOrAssembly => myMethodInfo.IsFamilyOrAssembly;
    public override bool IsHideBySig => myMethodInfo.IsHideBySig;

    public override ProvidedType DeclaringType =>
      ProxyProvidedType.Create(
        RdProvidedMethodInfoProcessModel.DeclaringType.Sync(EntityId),
        myProcessModel,
        myContext);

    public override ProvidedType ReturnType =>
      ProxyProvidedType.Create(RdProvidedMethodInfoProcessModel.ReturnType.Sync(EntityId), myProcessModel, myContext);

    public override ProvidedParameterInfo[] GetParameters() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedMethodInfoProcessModel.GetParameters
        .Sync(EntityId)
        .Select(t => ProxyProvidedParameterInfo.Create(t, myProcessModel, myContext))
        .ToArray();

    public override ProvidedType[] GetGenericArguments() =>
      // ReSharper disable once CoVariantArrayConversion
      RdProvidedMethodInfoProcessModel.GetGenericArguments
        .Sync(EntityId)
        .Select(t => ProxyProvidedType.Create(t, myProcessModel, myContext))
        .ToArray();
  }
}
