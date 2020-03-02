using System.Linq;
using JetBrains.Annotations;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedParameterInfo : ProvidedParameterInfo
  {
    private readonly RdProvidedParameterInfo myParameterInfo;
    private readonly ProvidedTypeContext myCtxt;

    public ProxyProvidedParameterInfo(RdProvidedParameterInfo parameterInfo, ProvidedTypeContext ctxt) : base(
      typeof(string).GetMethods().First().ReturnParameter, ctxt)
    {
      myParameterInfo = parameterInfo;
      myCtxt = ctxt;
    }

    [ContractAnnotation("null => null")]
    public static ProxyProvidedParameterInfo CreateNoContext(RdProvidedParameterInfo parameter) =>
      parameter == null ? null : new ProxyProvidedParameterInfo(parameter, ProvidedTypeContext.Empty);

    [ContractAnnotation("parameter:null => null")]
    public static ProxyProvidedParameterInfo Create(RdProvidedParameterInfo parameter, ProvidedTypeContext ctxt) =>
      parameter == null ? null : new ProxyProvidedParameterInfo(parameter, ctxt);

    public override string Name => myParameterInfo.Name;
    public override bool IsIn => myParameterInfo.IsIn;
    public override bool IsOptional => myParameterInfo.IsOptional;
    public override bool IsOut => myParameterInfo.IsOut;
    public override bool HasDefaultValue => myParameterInfo.HasDefaultValue;
    public override ProvidedType ParameterType => ProxyProvidedType.Create(myParameterInfo.ParameterType, myCtxt);
  }
}
