using System.Linq;
using JetBrains.Annotations;
using JetBrains.Core;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedParameterInfo : ProvidedParameterInfo
  {
    private readonly RdProvidedParameterInfo myParameterInfo;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly ProvidedTypeContext myContext;
    
      public ProxyProvidedParameterInfo(
        RdProvidedParameterInfo parameterInfo,
        RdFSharpTypeProvidersLoaderModel processModel,
        ProvidedTypeContext context) : base(
      typeof(string).GetMethods().First().ReturnParameter, context)
    {
      myParameterInfo = parameterInfo;
      myProcessModel = processModel;
      myContext = context;
    }

    [ContractAnnotation("parameter:null => null")]
    public static ProxyProvidedParameterInfo CreateNoContext(
      RdProvidedParameterInfo parameter,
      RdFSharpTypeProvidersLoaderModel processModel) =>
      parameter == null ? null : new ProxyProvidedParameterInfo(parameter, processModel, ProvidedTypeContext.Empty);

    [ContractAnnotation("parameter:null => null")]
    public static ProxyProvidedParameterInfo Create(
      RdProvidedParameterInfo parameter,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context) =>
      parameter == null ? null : new ProxyProvidedParameterInfo(parameter, processModel, context);

    public override string Name => myParameterInfo.Name;
    public override bool IsIn => myParameterInfo.IsIn;
    public override bool IsOptional => myParameterInfo.IsOptional;
    public override bool IsOut => myParameterInfo.IsOut;
    public override bool HasDefaultValue => myParameterInfo.HasDefaultValue;

    public override ProvidedType ParameterType =>
      ProxyProvidedType.Create(myParameterInfo.ParameterType.Sync(Unit.Instance), myProcessModel, myContext);
  }
}
