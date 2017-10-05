using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
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