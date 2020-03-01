using System.Linq;
using JetBrains.Annotations;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedMethodInfo : ProvidedMethodInfo
  {
    private readonly RdProvidedMethodInfo myMethodInfo;

    private ProxyProvidedMethodInfo(RdProvidedMethodInfo methodInfo) : base(typeof(string).GetMethods().First(),
      ProvidedTypeContext.Empty)
    {
      myMethodInfo = methodInfo;
    }

    [ContractAnnotation("null => null")]
    public static ProxyProvidedMethodInfo Create(RdProvidedMethodInfo methodInfo) =>
      methodInfo == null ? null : new ProxyProvidedMethodInfo(methodInfo);

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
    public override ProvidedType DeclaringType => ProxyProvidedType.Create(myMethodInfo.DeclaringType);
    public override ProvidedType ReturnType => ProxyProvidedType.Create(myMethodInfo.ReturnType);

    public override ProvidedParameterInfo[] GetParameters()
    {
      return myMethodInfo.GetParameters
        .Sync(Core.Unit.Instance)
        .Select(ProxyProvidedParameterInfo.Create)
        .ToArray();
    }

    public override ProvidedType[] GetGenericArguments()
    {
      return myMethodInfo.GetGenericArguments
        .Sync(Core.Unit.Instance)
        .Select(ProxyProvidedType.Create)
        .ToArray();
    }
  }
}
