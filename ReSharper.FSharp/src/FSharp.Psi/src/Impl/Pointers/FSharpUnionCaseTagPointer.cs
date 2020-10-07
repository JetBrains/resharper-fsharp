using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public class FSharpUnionCaseTagPointer : FSharpGeneratedElementPointerBase<FSharpUnionCaseTag, IUnionCase>
  {
    public FSharpUnionCaseTagPointer(FSharpUnionCaseTag tag) : base(tag)
    {
    }

    public override FSharpUnionCaseTag CreateGenerated(IUnionCase unionCase) =>
      new FSharpUnionCaseTag(unionCase);
  }
}
