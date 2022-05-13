using System.Xml;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpProvidedParameter : FSharpProvidedMember<ProvidedMemberInfo>, IParameter
  {
    private readonly ProvidedParameterInfo myInfo;

    public FSharpProvidedParameter(ProvidedParameterInfo info, IParametersOwner method)
      : base(null, method.GetContainingType())
    {
      myInfo = info;
      ContainingParametersOwner = method;
    }

    public new XmlNode GetXMLDoc(bool inherit) => myInfo.GetXmlDoc(this);
    public override string ShortName => myInfo.Name;
    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.PARAMETER;
    public IType Type => myInfo.ParameterType.MapType(Module);

    public DefaultValue GetDefaultValue() =>
      myInfo.HasDefaultValue
        ? new DefaultValue(Type, new ConstantValue(myInfo.RawDefaultValue, type: Type))
        : DefaultValue.BAD_VALUE;

    public ParameterKind Kind =>
      myInfo switch
      {
        { IsIn: true } => ParameterKind.INPUT,
        { IsOut: true } => ParameterKind.OUTPUT,
        _ => ParameterKind.VALUE
      };

    public bool IsParameterArray => false;
    public bool IsValueVariable => false;
    public bool IsOptional => myInfo.IsOptional;
    public bool IsVarArg => false;
    public IParametersOwner ContainingParametersOwner { get; }
  }
}
