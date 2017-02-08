using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  class Whitespace : WhitespaceBase
  {
    public Whitespace([NotNull] IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      : base(FSharpTokenType.WHITESPACE, buffer, startOffset, endOffset)
    {
    }

    public override bool IsNewLine => false;
  }
}