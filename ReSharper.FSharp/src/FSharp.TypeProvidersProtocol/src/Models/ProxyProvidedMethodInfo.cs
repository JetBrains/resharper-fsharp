using System.Linq;
using FSharp.Compiler;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;

namespace JetBrains.ReSharper.Plugins.FSharp.Models
{
  public class ProxyProvidedMethodInfo: ExtensionTyping.ProvidedMethodInfo
  {
    private readonly RdProvidedMethodInfo myMethodInfo;

    private ProxyProvidedMethodInfo(RdProvidedMethodInfo methodInfo) : base(typeof(string).GetMethods().First(), ExtensionTyping.ProvidedTypeContext.Empty)
    {
      myMethodInfo = methodInfo;
    }

    public override string Name => myMethodInfo.Name;
    public override ExtensionTyping.ProvidedType DeclaringType => ProxyProvidedType.Create(myMethodInfo.DeclaringType);
    public override bool IsAbstract => myMethodInfo.IsAbstract;
    public override bool IsConstructor => myMethodInfo.IsConstructor;
    public override bool IsFamily => myMethodInfo.IsFamily;
    public override bool IsFinal => myMethodInfo.IsFinal;
    public override bool IsPublic => myMethodInfo.IsPublic;
    public override bool IsStatic => myMethodInfo.IsStatic;
    public override bool IsVirtual => myMethodInfo.IsVirtual;
    public override int MetadataToken { get; }
    public override ExtensionTyping.ProvidedType ReturnType { get; }
    public override bool IsGenericMethod { get; }
    public override bool IsFamilyAndAssembly { get; }
    public override bool IsFamilyOrAssembly { get; }
    public override bool IsHideBySig { get; }
    public override ExtensionTyping.ProvidedParameterInfo[] GetParameters()
    {
      return base.GetParameters();
    }

    public override ExtensionTyping.ProvidedType[] GetGenericArguments()
    {
      return base.GetGenericArguments();
    }
  }
}
