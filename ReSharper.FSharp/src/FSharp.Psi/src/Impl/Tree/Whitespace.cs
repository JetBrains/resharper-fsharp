using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
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