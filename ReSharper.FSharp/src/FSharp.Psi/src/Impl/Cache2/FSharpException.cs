using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  internal class FSharpException : FSharpSimpleTypeBase
  {
    public FSharpException([NotNull] IClassPart part) : base(part)
    {
    }

    protected override bool OverridesCompareTo => false;
    protected override bool OverridesToString => false;
  }
}