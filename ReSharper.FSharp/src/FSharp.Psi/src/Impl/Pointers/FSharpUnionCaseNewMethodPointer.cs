using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public class FSharpUnionCaseNewMethodPointer : FSharpGeneratedElementPointerBase<FSharpUnionCaseNewMethod, IUnionCase>
  {
    public FSharpUnionCaseNewMethodPointer(FSharpUnionCaseNewMethod unionCaseNewMethod) : base(unionCaseNewMethod)
    {
    }

    public override FSharpUnionCaseNewMethod CreateGenerated(IUnionCase unionCase) =>
      new FSharpUnionCaseNewMethod(unionCase);
  }
}
