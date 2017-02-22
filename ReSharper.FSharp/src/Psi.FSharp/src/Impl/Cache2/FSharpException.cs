using JetBrains.Annotations;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpException : FSharpClassLikeElement<ExceptionPart>
  {
    public FSharpException([NotNull] IClassPart part) : base(part)
    {
    }
  }
}