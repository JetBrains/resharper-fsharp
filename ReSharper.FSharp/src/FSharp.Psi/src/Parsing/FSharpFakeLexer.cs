using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  /// <summary>
  /// Creates a single token for the whole file.
  /// FSharpParser uses FSharpLexer which is aware of current project configuration needed for conditional compilation.
  /// </summary>
  public class FSharpFakeLexer : ILexer
  {
    public FSharpFakeLexer(IBuffer buffer)
    {
      Buffer = buffer;
    }

    public void Start()
    {
      CurrentPosition = 0;
      TokenStart = 0;
      TokenEnd = Buffer.Length;
      TokenType = FSharpTokenType.FAKE;
    }

    public void Advance()
    {
      TokenType = null;
    }

    public object CurrentPosition { get; set; }
    public TokenNodeType TokenType { get; private set; }
    public int TokenStart { get; private set; }
    public int TokenEnd { get; private set; }
    public IBuffer Buffer { get; }
  }
}