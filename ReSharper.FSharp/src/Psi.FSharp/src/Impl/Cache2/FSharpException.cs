using JetBrains.Annotations;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpException : FSharpSimpleTypeBase
  {
    public FSharpException([NotNull] IClassPart part) : base(part)
    {
    }

    protected override bool ImplementsCompareTo() => false;
  }
}