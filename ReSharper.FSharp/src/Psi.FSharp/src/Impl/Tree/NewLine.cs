using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  class NewLine : WhitespaceBase
  {
    public NewLine([NotNull] IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
      : base(FSharpTokenType.NEW_LINE, buffer, startOffset, endOffset)
    {
    }

    public override bool IsNewLine => true;
  }
}