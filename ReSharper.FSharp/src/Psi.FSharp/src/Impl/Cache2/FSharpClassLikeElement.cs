using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class FSharpClassLikeElement<TPart> : Class where TPart : Part
  {
    public FSharpClassLikeElement([NotNull] IClassPart part) : base(part)
    {
    }

    protected override bool AcceptsPart(TypePart part)
    {
      return part is TPart;
    }
  }
}