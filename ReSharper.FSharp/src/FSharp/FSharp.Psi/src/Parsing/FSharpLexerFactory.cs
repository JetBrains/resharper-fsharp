using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public class FSharpLexerFactory : ILexerFactory
  {
    public ILexer CreateLexer(IBuffer buffer) =>
      new FSharpLexer(buffer);
  }
}
