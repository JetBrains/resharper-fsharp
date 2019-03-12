using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public class IsUnionCasePropertyPointer : FSharpGeneratedElementPointerBase<IsUnionCaseProperty, IUnionCase>
  {
    public IsUnionCasePropertyPointer(IsUnionCaseProperty isUnionCaseProperty) : base(isUnionCaseProperty)
    {
    }

    public override IsUnionCaseProperty CreateGenerated(IUnionCase unionCase) =>
      new IsUnionCaseProperty(unionCase);
  }
}
