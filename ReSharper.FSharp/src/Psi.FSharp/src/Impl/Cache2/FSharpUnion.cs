using JetBrains.Annotations;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpUnion : FSharpClassLikeElement<UnionPart>
  {
    public FSharpUnion([NotNull] IClassPart part) : base(part)
    {
    }
  }
}