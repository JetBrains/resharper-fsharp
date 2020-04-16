using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LiteralExpr
  {
    private static readonly NodeTypeSet LiteralTokenTypes =
      new NodeTypeSet(
        FSharpTokenType.TRUE,
        FSharpTokenType.FALSE,
        FSharpTokenType.IEEE32,
        FSharpTokenType.IEEE64,
        FSharpTokenType.BYTE,
        FSharpTokenType.INT16,
        FSharpTokenType.INT32,
        FSharpTokenType.INT64,
        FSharpTokenType.SBYTE,
        FSharpTokenType.UINT16,
        FSharpTokenType.UINT32,
        FSharpTokenType.UINT64,
        FSharpTokenType.CHARACTER_LITERAL,
        FSharpTokenType.STRING,
        FSharpTokenType.VERBATIM_STRING,
        FSharpTokenType.TRIPLE_QUOTED_STRING
      );

    public override bool IsConstantValue()
    {
      var tokenType = Literal?.GetTokenType();
      return LiteralTokenTypes[tokenType];
    }
  }
}
