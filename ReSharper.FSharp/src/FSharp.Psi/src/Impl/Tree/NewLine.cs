using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  class NewLine : WhitespaceBase
  {
    public NewLine(string text) : base(FSharpTokenType.NEW_LINE, text)
    {
    }

    public override bool IsNewLine => true;
  }
}