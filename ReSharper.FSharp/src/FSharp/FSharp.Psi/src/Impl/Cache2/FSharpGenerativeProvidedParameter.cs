using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve.Managed;
using static FSharp.Compiler.TypeProviders;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpGenerativeProvidedParameter : FSharpGenerativeProvidedElement<ProvidedParameterInfo>, IParameter
  {
    public FSharpGenerativeProvidedParameter(ProvidedParameterInfo info, IParametersOwner method)
      : base(info, method.GetContainingType())
    {
      ContainingParametersOwner = method;
    }

    public override string ShortName => Info.Name;
    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.PARAMETER;
    public IType Type => Info.ParameterType.MapType(Module);

    public DefaultValue GetDefaultValue() =>
      Info.HasDefaultValue
        ? new DefaultValue(Type, ConstantValue.Create(Info.RawDefaultValue, type: Type))
        : DefaultValue.BAD_VALUE;

    public ParameterKind Kind =>
      Info switch
      {
        { IsIn: true } => ParameterKind.INPUT,
        { IsOut: true } => ParameterKind.OUTPUT,
        _ => ParameterKind.VALUE
      };

    public bool IsParams => IsParameterArray;

    public bool IsParameterArray =>
      Info is ProxyProvidedParameterInfoWithContext x &&
      x.GetAttributeConstructorArgs(null, "System.ParamArrayAttribute") != null;

    public bool IsParameterCollection => false;

    public bool IsValueVariable => false;
    public bool IsOptional => Info.IsOptional;
    public bool IsVarArg => false;
    public ScopedKind GetScope(IResolveContext context = null) => ScopedKind.None;
    public IParametersOwner ContainingParametersOwner { get; }
  }
}
