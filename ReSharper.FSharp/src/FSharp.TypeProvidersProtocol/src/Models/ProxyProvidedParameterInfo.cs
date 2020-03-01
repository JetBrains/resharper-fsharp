using JetBrains.Annotations;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedParameterInfo : ProvidedParameterInfo
  {
    private readonly RdProvidedParameterInfo myParameterInfo;

    public ProxyProvidedParameterInfo(RdProvidedParameterInfo parameterInfo) : base(null, ProvidedTypeContext.Empty)
    {
      myParameterInfo = parameterInfo;
    }

    [ContractAnnotation("null => null")]
    public static ProxyProvidedParameterInfo Create(RdProvidedParameterInfo parameter) =>
      parameter == null ? null : new ProxyProvidedParameterInfo(parameter);

    public override string Name => myParameterInfo.Name;
    public override bool IsIn => myParameterInfo.IsIn;
    public override bool IsOptional => myParameterInfo.IsOptional;
    public override bool IsOut => myParameterInfo.IsOut;
    public override bool HasDefaultValue => myParameterInfo.HasDefaultValue;
    public override ProvidedType ParameterType => ProxyProvidedType.Create(myParameterInfo.ParameterType);
  }
}
