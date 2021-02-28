using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public class FSharpIdentifierToken : FSharpToken, IFSharpIdentifierToken
  {
    public FSharpIdentifierToken(NodeType nodeType, string text) : base(nodeType, text)
    {
    }

    public FSharpIdentifierToken(string text) : base(FSharpTokenType.IDENTIFIER, text)
    {
    }

    public string Name => GetText().RemoveBackticks();

    public ITokenNode IdentifierToken => this;
    public TreeTextRange NameRange => this.GetTreeTextRange();
  }
}
