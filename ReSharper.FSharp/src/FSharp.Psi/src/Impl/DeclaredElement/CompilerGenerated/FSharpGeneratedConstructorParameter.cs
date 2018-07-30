using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public class FSharpGeneratedConstructorParameter : FSharpGeneratedElementBase, IParameter
  {
    [NotNull]
    protected IConstructor Constructor { get; }

    [CanBeNull]
    protected ITypeOwner Origin { get; }

    public FSharpGeneratedConstructorParameter([NotNull] IConstructor constructor, [CanBeNull] ITypeOwner origin)
    {
      Constructor = constructor;
      Origin = origin;
    }

    public override string ShortName =>
      Origin?.ShortName.Decapitalize() ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public IType Type =>
      Origin?.Type ?? TypeFactory.CreateUnknownType(Module);

    public override bool IsValid() =>
      Constructor.IsValid() && Origin != null && Origin.IsValid();

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.PARAMETER;

    public IParametersOwner ContainingParametersOwner => Constructor;
    protected override IClrDeclaredElement ContainingElement => Constructor;
    public override ITypeMember GetContainingTypeMember() => Constructor;
    public override ITypeElement GetContainingType() => Constructor.GetContainingType();

    public IList<IAttributeInstance> GetAttributeInstances(bool inherit) =>
      EmptyList<IAttributeInstance>.Instance;

    public IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit) =>
      EmptyList<IAttributeInstance>.Instance;

    public bool HasAttributeInstance(IClrTypeName clrName, bool inherit) => false;

    public ParameterKind Kind => ParameterKind.VALUE;
    public DefaultValue GetDefaultValue() => DefaultValue.BAD_VALUE;

    public bool IsParameterArray => false;
    public bool IsValueVariable => false;
    public bool IsOptional => false;
    public bool IsVarArg => false;

    public override bool Equals(object obj) =>
      obj is FSharpGeneratedConstructorParameter param && ShortName == param.ShortName &&
      ContainingElement.Equals(param.ContainingElement);

    public override int GetHashCode() => ShortName.GetHashCode();
  }
}
