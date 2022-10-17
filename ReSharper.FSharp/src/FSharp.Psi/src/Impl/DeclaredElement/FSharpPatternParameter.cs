using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpPatternParameter : FSharpGeneratedElementBase, IFSharpParameter
  {
    public FSharpPatternParameter(bool isGenerated)
    {
      IsGenerated = isGenerated;
    }

    public override string ShortName { get; }
    public override ITypeElement GetContainingType()
    {
      throw new System.NotImplementedException();
    }

    public override ITypeMember GetContainingTypeMember()
    {
      throw new System.NotImplementedException();
    }

    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.PARAMETER;

    protected override IClrDeclaredElement ContainingElement { get; }
    public IType Type { get; }

    public DefaultValue GetDefaultValue() => this.GetParameterDefaultValue();
    public ParameterKind Kind => Symbol.GetParameterKind();

    public bool IsOptional => Symbol.HasAttributeInstance(PredefinedType.OPTIONAL_ATTRIBUTE_CLASS);

    public bool IsParameterArray => this.IsParameterArray();

    public bool IsValueVariable => false;
    public bool IsVarArg => false;

    public IParametersOwner ContainingParametersOwner { get; }
    public (int group, int index) Position { get; }
    public bool IsErased { get; }
    public bool IsGenerated { get; }

    public FSharpParameter Symbol { get; }

    public override bool Equals(object obj) =>
      obj is IFSharpParameter fsParam && fsParam.Position == Position &&
      ContainingParametersOwner is { } owner && owner.Equals(fsParam.ContainingParametersOwner);

    public override int GetHashCode() =>
      Position.GetHashCode();
  }
}
