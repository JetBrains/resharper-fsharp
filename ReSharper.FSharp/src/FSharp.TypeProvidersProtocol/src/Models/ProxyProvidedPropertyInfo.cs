using System.Linq;
using FSharp.Compiler;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using ProvidedPropertyInfo = FSharp.Compiler.ExtensionTyping.ProvidedPropertyInfo;
using ProvidedTypeContext = FSharp.Compiler.ExtensionTyping.ProvidedTypeContext;
using ProvidedType = FSharp.Compiler.ExtensionTyping.ProvidedType;

namespace JetBrains.ReSharper.Plugins.FSharp.Models
{
  public class ProxyProvidedPropertyInfo : ProvidedPropertyInfo
  {
    private readonly RdProvidedPropertyInfo myPropertyInfo;

    private ProxyProvidedPropertyInfo(RdProvidedPropertyInfo propertyInfo) : base(
      typeof(string).GetProperties().First(), ProvidedTypeContext.Empty)
    {
      myPropertyInfo = propertyInfo;
    }

    public static ProxyProvidedPropertyInfo Create(RdProvidedPropertyInfo propertyInfo)
    {
      return new ProxyProvidedPropertyInfo(propertyInfo);
    }

    public override string Name => myPropertyInfo.Name;
    public override bool CanRead => myPropertyInfo.CanRead;
    public override bool CanWrite => myPropertyInfo.CanWrite;
    public override ProvidedType DeclaringType => ProxyProvidedType.Create(myPropertyInfo.DeclaringType);
    public override ProvidedType PropertyType => ProxyProvidedType.Create(myPropertyInfo.PropertyType);
    public override ExtensionTyping.ProvidedMethodInfo GetGetMethod()
    {
      return ProxyProvidedMethodInfo.Create(myPropertyInfo.GetGetMethod.Sync(Core.Unit.Instance));
    }

    public override ExtensionTyping.ProvidedMethodInfo GetSetMethod()
    {
      return base.GetSetMethod();
    }

    public override ExtensionTyping.ProvidedParameterInfo[] GetIndexParameters()
    {
      return base.GetIndexParameters();
    }
  }
}
