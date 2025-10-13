using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public class FSharpUnionCaseNewMethodPointer(FSharpUnionCaseNewMethod unionCaseNewMethod)
    : FSharpGeneratedElementPointerBase<FSharpUnionCaseNewMethod, IFSharpUnionCase>(unionCaseNewMethod)
  {
    public override FSharpUnionCaseNewMethod CreateGenerated(IFSharpUnionCase unionCase) => new(unionCase);
  }
}
