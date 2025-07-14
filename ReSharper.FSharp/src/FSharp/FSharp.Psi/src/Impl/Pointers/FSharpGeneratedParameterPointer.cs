using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public class FSharpGeneratedParameterPointer(FSharpGeneratedParameter element, bool addPrefix)
    : FSharpGeneratedElementPointerBase<FSharpGeneratedParameter, IFSharpFieldProperty>(element)
  {
    public override FSharpGeneratedParameter CreateGenerated(IFSharpFieldProperty field) =>
      field.ContainingType.GetGeneratedConstructor() is { } constructor
        ? new FSharpGeneratedParameter(constructor, field, addPrefix)
        : null;
  }
}
