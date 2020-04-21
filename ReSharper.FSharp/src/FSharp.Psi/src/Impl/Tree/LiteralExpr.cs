using System;
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
          var text = literal.GetText();
          var literalBase = GetIntegerDigitsStartOffsetAndBase(text);
          var startOffset = literalBase == IntBase.Decimal ? 0 : 2;
          var literalText = text.Substring(startOffset).Replace("_", string.Empty);

          try
          {
            var result = Convert.ToInt32(literalText, (int) literalBase);
            return new ConstantValue(result, GetPsiModule().GetPredefinedType().Int);
          }
          catch (Exception)
          {
            return ConstantValue.BAD_VALUE;
          }
        }

        return ConstantValue.BAD_VALUE;
      }
    }

    private static IntBase GetIntegerDigitsStartOffsetAndBase(string literalText)
    {
      if (literalText.Length <= 2 || literalText[0] != '0')
        return IntBase.Decimal;

      return literalText[1] switch
      {
        'X' => IntBase.Hexadecimal,
        'x' => IntBase.Hexadecimal,
        'B' => IntBase.Binary,
        'b' => IntBase.Binary,
        'O' => IntBase.Octal,
        'o' => IntBase.Octal,
        _ => IntBase.Decimal
      };
    }

    private enum IntBase
    {
      Binary = 2,
      Octal = 8,
      Decimal = 10,
      Hexadecimal = 16
    }
  }
}
