using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  class Whitespace : WhitespaceBase
  {
    public Whitespace(string text) : base(FSharpTokenType.WHITESPACE, text)
    {
    }

    public override bool IsNewLine => false;
  }
}