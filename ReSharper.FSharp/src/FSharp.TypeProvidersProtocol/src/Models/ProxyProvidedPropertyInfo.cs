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

    private ProxyProvidedPropertyInfo(RdProvidedPropertyInfo propertyInfo) : base(
      typeof(string).GetProperties().First(), ProvidedTypeContext.Empty)
    {
      myPropertyInfo = propertyInfo;
    }

    [ContractAnnotation("null => null")]
    public static ProxyProvidedPropertyInfo Create(RdProvidedPropertyInfo propertyInfo) =>
      propertyInfo == null ? null : new ProxyProvidedPropertyInfo(propertyInfo);

    public override string Name => myPropertyInfo.Name;
    public override bool CanRead => myPropertyInfo.CanRead;
    public override bool CanWrite => myPropertyInfo.CanWrite;
    public override ProvidedType DeclaringType => ProxyProvidedType.Create(myPropertyInfo.DeclaringType);
    public override ProvidedType PropertyType => ProxyProvidedType.Create(myPropertyInfo.PropertyType);

    public override ProvidedMethodInfo GetGetMethod() =>
      ProxyProvidedMethodInfo.Create(myPropertyInfo.GetGetMethod.Sync(Core.Unit.Instance));

    public override ProvidedMethodInfo GetSetMethod() =>
      ProxyProvidedMethodInfo.Create(myPropertyInfo.GetSetMethod.Sync(Core.Unit.Instance));

    public override ProvidedParameterInfo[] GetIndexParameters()
    {
      return myPropertyInfo.GetIndexParameters
        .Sync(Unit.Instance)
        .Select(ProxyProvidedParameterInfo.Create)
        .ToArray();
    }
  }
}
