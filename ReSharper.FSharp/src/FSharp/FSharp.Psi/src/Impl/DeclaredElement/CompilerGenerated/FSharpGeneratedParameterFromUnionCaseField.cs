using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

public class FSharpGeneratedParameterFromUnionCaseField(IFSharpParameterOwner owner, IUnionCaseField field, int index)
  : FSharpGeneratedParameter(owner, field, true), IFSharpParameter
{
  public override IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
    new FSharpGeneratedParameterFromUnionCaseFieldPointer(this, index);

  private class FSharpGeneratedParameterFromUnionCaseFieldPointer(FSharpGeneratedParameterFromUnionCaseField element, int index)
    : FSharpGeneratedElementPointerBase<FSharpGeneratedParameterFromUnionCaseField, IUnionCaseField>(element)
  {
    public override FSharpGeneratedParameterFromUnionCaseField CreateGenerated(IUnionCaseField field) =>
      field.ContainingType.GetGeneratedConstructor() is { } constructor
        ? new FSharpGeneratedParameterFromUnionCaseField(constructor, field, index)
        : null;
  }

  public FSharpParameterIndex FSharpIndex => new(0, @field.Index);
}
