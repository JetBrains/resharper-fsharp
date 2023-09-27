using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public class FSharpUnionCaseClassPointer : FSharpGeneratedElementPointerBase<FSharpUnionCaseClass, IUnionCase>
  {
    public FSharpUnionCaseClassPointer(FSharpUnionCaseClass nestedType) : base(nestedType)
    {
    }

    public override FSharpUnionCaseClass CreateGenerated(IUnionCase unionCase) =>
      unionCase.NestedType;
  }
}
