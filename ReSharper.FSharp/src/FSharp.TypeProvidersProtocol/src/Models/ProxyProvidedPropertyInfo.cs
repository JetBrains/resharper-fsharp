using System.Linq;
using JetBrains.Annotations;
using JetBrains.Core;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedPropertyInfo : ProvidedPropertyInfo
  {
    private readonly RdProvidedPropertyInfo myPropertyInfo;
    private readonly ProvidedTypeContext myCtxt;

    private ProxyProvidedPropertyInfo(RdProvidedPropertyInfo propertyInfo, ProvidedTypeContext ctxt) : base(
      typeof(string).GetProperties().First(), ctxt)
    {
      myPropertyInfo = propertyInfo;
      myCtxt = ctxt;
    }

    [ContractAnnotation("null => null")]
    public static ProxyProvidedPropertyInfo CreateNoContext(RdProvidedPropertyInfo propertyInfo) =>
      propertyInfo == null ? null : new ProxyProvidedPropertyInfo(propertyInfo, ProvidedTypeContext.Empty);

    [ContractAnnotation("propertyInfo:null => null")]
    public static ProxyProvidedPropertyInfo Create(RdProvidedPropertyInfo propertyInfo, ProvidedTypeContext ctxt) =>
      propertyInfo == null ? null : new ProxyProvidedPropertyInfo(propertyInfo, ctxt);

    public override string Name => myPropertyInfo.Name;
    public override bool CanRead => myPropertyInfo.CanRead;
    public override bool CanWrite => myPropertyInfo.CanWrite;

    public override ProvidedType DeclaringType =>
      ProxyProvidedType.Create(myPropertyInfo.DeclaringType.Sync(Unit.Instance), myCtxt);

    public override ProvidedType PropertyType =>
      ProxyProvidedType.Create(myPropertyInfo.PropertyType.Sync(Unit.Instance), myCtxt);

    public override ProvidedMethodInfo GetGetMethod() =>
      ProxyProvidedMethodInfo.Create(myPropertyInfo.GetGetMethod.Sync(Core.Unit.Instance), myCtxt);

    public override ProvidedMethodInfo GetSetMethod() =>
      ProxyProvidedMethodInfo.Create(myPropertyInfo.GetSetMethod.Sync(Core.Unit.Instance), myCtxt);

    public override ProvidedParameterInfo[] GetIndexParameters()
    {
      return myPropertyInfo.GetIndexParameters
        .Sync(Unit.Instance)
        .Select(t => ProxyProvidedParameterInfo.Create(t, myCtxt))
        .ToArray();
    }
  }
}
