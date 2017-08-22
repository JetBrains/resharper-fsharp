using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  internal class FSharpRecord : FSharpSimpleTypeBase
  {
    public FSharpRecord([NotNull] IClassPart part) : base(part)
    {
    }
  }
}