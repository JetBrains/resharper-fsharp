using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public class FSharpGeneratedParameter : FSharpGeneratedElementBase, IParameter,
    IFSharpGeneratedFromOtherElement
  {
    [NotNull] protected IParametersOwner Owner { get; }

    [CanBeNull] internal ITypeOwner Origin { get; }

    public FSharpGeneratedParameter([NotNull] IParametersOwner owner, [CanBeNull] ITypeOwner origin)
    {
      Owner = owner;
      Origin = origin;
    }

    public override string ShortName =>
      Origin?.ShortName.Decapitalize() ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public IType Type =>
      Origin?.Type ?? TypeFactory.CreateUnknownType(Module);

    public override bool IsValid() =>
      Owner.IsValid() && Origin != null && Origin.IsValid();

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.PARAMETER;

    public IParametersOwner ContainingParametersOwner => Owner;
    protected override IClrDeclaredElement ContainingElement => Owner;
    public override ITypeMember GetContainingTypeMember() => Owner as ITypeMember;
    public override ITypeElement GetContainingType() => Owner.GetContainingType();

    public ParameterKind Kind => ParameterKind.VALUE;
    public DefaultValue GetDefaultValue() => DefaultValue.BAD_VALUE;

    public bool IsParameterArray => false;
    public bool IsValueVariable => false;
    public bool IsOptional => false;
    public bool IsVarArg => false;

    public override bool Equals(object obj) =>
      obj is FSharpGeneratedParameter param && ShortName == param.ShortName &&
      ContainingElement.Equals(param.ContainingElement);

    public override int GetHashCode() => ShortName.GetHashCode();
    public IClrDeclaredElement OriginElement => Origin;
    public bool IsReadOnly => false;

    public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
      new FSharpGeneratedParameterPointer(this);
  }
}
