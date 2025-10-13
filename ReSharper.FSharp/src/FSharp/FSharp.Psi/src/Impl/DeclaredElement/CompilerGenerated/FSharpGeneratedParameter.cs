using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resolve.Managed;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public class FSharpGeneratedParameter([NotNull] IFSharpParameterOwner owner, [NotNull] IFSharpFunctionalTypeField origin, bool addPrefix)
    : FSharpGeneratedElementBase, IFSharpGeneratedFromOtherElement, IFSharpParameter
  {
    [NotNull] protected IParametersOwner Owner { get; } = owner;

    [NotNull] internal IFSharpFunctionalTypeField Origin { get; } = origin;

    public override string ShortName
    {
      get
      {
        var name = Origin.ShortName.Decapitalize();
        return addPrefix ? "_" + name : name;
      }
    }

    public IType Type => Origin.Type;

    public override bool IsValid() =>
      Owner.IsValid() && Origin.IsValid();

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.PARAMETER;

    public ScopedKind GetScope(IResolveContext context = null) => ScopedKind.None;
    public IParametersOwner ContainingParametersOwner => Owner;
    protected override IClrDeclaredElement ContainingElement => Owner;
    public override ITypeMember GetContainingTypeMember() => Owner as ITypeMember;
    public override ITypeElement GetContainingType() => Owner.GetContainingType();

    public ParameterKind Kind => ParameterKind.VALUE;
    public DefaultValue GetDefaultValue() => DefaultValue.BAD_VALUE;

    public bool IsParams => false;
    public bool IsParameterArray => false;
    public bool IsParameterCollection => false;
    public bool IsValueVariable => false;
    public bool IsOptional => false;
    public bool IsVarArg => false;

    public override bool Equals(object obj) =>
      obj is FSharpGeneratedParameter param && ShortName == param.ShortName &&
      ContainingElement.Equals(param.ContainingElement);

    public override int GetHashCode() => ShortName.GetHashCode();
    public IClrDeclaredElement OriginElement => Origin;

    public virtual IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
      new FSharpGeneratedParameterPointer(this, addPrefix);

    public FSharpParameterIndex FSharpIndex => new(0, Origin.Index);
  }
}
