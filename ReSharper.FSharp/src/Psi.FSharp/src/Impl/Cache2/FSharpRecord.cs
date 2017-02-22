using JetBrains.Annotations;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpRecord : FSharpClassLikeElement<RecordPart>
  {
    public FSharpRecord([NotNull] IClassPart part) : base(part)
    {
    }
  }
}