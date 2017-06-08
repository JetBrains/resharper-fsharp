using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Parts;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpException : FSharpClass
  {
    public FSharpException([NotNull] IClassPart part) : base(part)
    {
    }
  }
}