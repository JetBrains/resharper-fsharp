using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public class Whitespace : WhitespaceBase
  {
    public Whitespace(string text) : base(FSharpTokenType.WHITESPACE, text)
    {
    }

    public Whitespace(int length) : this(new string(' ', length))
    {
    }

    public Whitespace() : this(1)
    {
    }

    public override bool IsNewLine => false;
  }

  public class NewLine : WhitespaceBase
  {
    public NewLine(string text) : base(FSharpTokenType.NEW_LINE, text)
    {
    }

    public override bool IsNewLine => true;
  }

  public abstract class WhitespaceBase : FSharpToken, IWhitespaceNode
  {
    protected WhitespaceBase(NodeType nodeType, string text) : base(nodeType, text)
    {
    }

    public override bool IsFiltered() => true;

    public override string ToString() => base.ToString() + " spaces:" + "\"" + GetText() + "\"";
    public abstract bool IsNewLine { get; }
  }
}
