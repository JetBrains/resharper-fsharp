using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public class
    FSharpUnionCaseIsCasePropertyPointer : FSharpGeneratedElementPointerBase<FSharpUnionCaseIsCaseProperty, IFSharpUnionCase>
  {
    public FSharpUnionCaseIsCasePropertyPointer(FSharpUnionCaseIsCaseProperty isUnionCaseProperty)
      : base(isUnionCaseProperty)
    {
    }

    public override FSharpUnionCaseIsCaseProperty CreateGenerated(IFSharpUnionCase unionCase) => new(unionCase);
  }
}
