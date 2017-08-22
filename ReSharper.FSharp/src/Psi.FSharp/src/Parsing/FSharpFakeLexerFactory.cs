using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public class FSharpFakeLexerFactory : ILexerFactory
  {
    public static ILexerFactory Instance = new FSharpFakeLexerFactory();

    public ILexer CreateLexer(IBuffer buffer)
    {
      return new FSharpFakeLexer(buffer);
    }
  }
}