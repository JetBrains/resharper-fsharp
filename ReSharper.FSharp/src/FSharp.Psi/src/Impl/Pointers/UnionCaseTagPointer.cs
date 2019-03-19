using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers
{
  public class UnionCaseTagPointer : FSharpGeneratedElementPointerBase<UnionCaseTag, IUnionCase>
  {
    public UnionCaseTagPointer(UnionCaseTag tag) : base(tag)
    {
    }

    public override UnionCaseTag CreateGenerated(IUnionCase unionCase) =>
      new UnionCaseTag(unionCase);
  }
}
