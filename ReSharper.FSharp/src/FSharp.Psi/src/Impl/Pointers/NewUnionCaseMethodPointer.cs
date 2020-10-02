using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public class NewUnionCaseMethodPointer : FSharpGeneratedElementPointerBase<NewUnionCaseMethod, IUnionCaseWithFields>
  {
    public NewUnionCaseMethodPointer(NewUnionCaseMethod newUnionCaseMethod) : base(newUnionCaseMethod)
    {
    }

    public override NewUnionCaseMethod CreateGenerated(IUnionCaseWithFields unionCase) =>
      new NewUnionCaseMethod(unionCase);
  }
}
