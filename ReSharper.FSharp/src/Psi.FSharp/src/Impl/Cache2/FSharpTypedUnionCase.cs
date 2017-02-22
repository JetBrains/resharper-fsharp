using JetBrains.Annotations;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpTypedUnionCase : FSharpClassLikeElement<TypedUnionCasePart>
  {
    public FSharpTypedUnionCase([NotNull] IClassPart part) : base(part)
    {
    }
  }
}