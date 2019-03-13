using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public class FSharpGeneratedParameterPointer
    : FSharpGeneratedElementPointerBase<FSharpGeneratedParameter, IFSharpFieldProperty>
  {
    public FSharpGeneratedParameterPointer(FSharpGeneratedParameter element) : base(element)
    {
    }

    public override FSharpGeneratedParameter CreateGenerated(IFSharpFieldProperty field) =>
      field.GetContainingType().GetGeneratedConstructor() is IParametersOwner constructor
        ? new FSharpGeneratedParameter(constructor, field)
        : null;
  }
}
