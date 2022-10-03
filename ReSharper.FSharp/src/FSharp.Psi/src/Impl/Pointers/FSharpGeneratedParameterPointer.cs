using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public class FSharpGeneratedParameterPointer
    : FSharpGeneratedElementPointerBase<FSharpGeneratedParameter, IFSharpFieldProperty>
  {
    private readonly bool myAddPrefix;

    public FSharpGeneratedParameterPointer(FSharpGeneratedParameter element, bool addPrefix) : base(element) =>
      myAddPrefix = addPrefix;

    public override FSharpGeneratedParameter CreateGenerated(IFSharpFieldProperty field) =>
      field.ContainingType.GetGeneratedConstructor() is { } constructor
        ? new FSharpGeneratedParameter(constructor, field, myAddPrefix)
        : null;
  }
}
