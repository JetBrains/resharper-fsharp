using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi;
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

    public override ConstantValue ConstantValue
    {
      get
      {
        var literal = Literal;
        if (literal == null)
          return ConstantValue.BAD_VALUE;

        var tokenType = literal.GetTokenType();
        if (tokenType == FSharpTokenType.INT32)
        {
          // todo: hex, octal, binary
          if (int.TryParse(literal.GetText(), out var result))
            return new ConstantValue(result, GetPsiModule().GetPredefinedType().Int);
        }

        return ConstantValue.BAD_VALUE;
      }
    }
  }
}
