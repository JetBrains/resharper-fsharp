using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public class
    FSharpUnionCaseIsCasePropertyPointer : FSharpGeneratedElementPointerBase<FSharpUnionCaseIsCaseProperty, IUnionCase>
  {
    public FSharpUnionCaseIsCasePropertyPointer(FSharpUnionCaseIsCaseProperty isUnionCaseProperty)
      : base(isUnionCaseProperty)
    {
    }

    public override FSharpUnionCaseIsCaseProperty CreateGenerated(IUnionCase unionCase) =>
      new FSharpUnionCaseIsCaseProperty(unionCase);
  }
}
