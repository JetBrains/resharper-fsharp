using JetBrains.Text;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing.Lexing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public class FSharpLexer : FSharpLexerGenerated
  {
    public FSharpLexer(IBuffer buffer) : base(buffer)
    {
    }

    public FSharpLexer(IBuffer buffer, int startOffset, int endOffset) : base(buffer, startOffset, endOffset)
    {
    }
  }
}
