using System;
using System.Linq;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache
{
  public class ProvidedParameterInfoWithCache:  ProvidedParameterInfo
  {
    private readonly ProvidedParameterInfo myParameterInfo;

    public ProvidedParameterInfoWithCache(ProvidedParameterInfo parameterInfo) : base(typeof(string).GetMethods().First().ReturnParameter, ProvidedTypeContext.Empty)
    {
      myParameterInfo = parameterInfo;
      
      myParameterType = new Lazy<ProvidedType>(() => myParameterInfo.ParameterType);
    }

    public override string Name => myParameterInfo.Name;
    public override bool IsIn => myParameterInfo.IsIn;
    public override bool IsOut => myParameterInfo.IsOut;
    public override bool IsOptional => myParameterInfo.IsOptional;
    public override bool HasDefaultValue => myParameterInfo.HasDefaultValue;
    public override ProvidedType ParameterType => myParameterType.Value;

    private readonly Lazy<ProvidedType> myParameterType;
  }
}
