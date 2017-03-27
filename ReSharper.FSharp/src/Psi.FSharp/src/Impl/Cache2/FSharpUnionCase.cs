using JetBrains.Annotations;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpUnionCase : FSharpClassLikeElement<UnionCasePart>
  {
    public FSharpUnionCase([NotNull] IClassPart part) : base(part)
    {
    }
  }
}