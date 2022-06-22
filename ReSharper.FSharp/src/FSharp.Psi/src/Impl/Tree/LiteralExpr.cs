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

    public override IType Type()
    {
      var psiModule = GetPsiModule();

      var tokenType = Literal?.GetTokenType();
      if (tokenType == null)
        return TypeFactory.CreateUnknownType(psiModule);

      var predefinedType = psiModule.GetPredefinedType();

      if (tokenType == FSharpTokenType.TRUE || tokenType == FSharpTokenType.FALSE)
        return predefinedType.Bool;

      if (tokenType == FSharpTokenType.STRING || tokenType == FSharpTokenType.VERBATIM_STRING ||
          tokenType == FSharpTokenType.TRIPLE_QUOTED_STRING)
        return predefinedType.String;

      if (tokenType == FSharpTokenType.CHARACTER_LITERAL)
        return predefinedType.Char;

      if (tokenType == FSharpTokenType.INT32)
        return predefinedType.Int;

      if (tokenType == FSharpTokenType.UINT32)
        return predefinedType.Uint;

      if (tokenType == FSharpTokenType.IEEE64)
        return predefinedType.Double;

      if (tokenType == FSharpTokenType.IEEE32)
        return predefinedType.Float;

      if (tokenType == FSharpTokenType.UINT64)
        return predefinedType.Ulong;

      if (tokenType == FSharpTokenType.INT16)
        return predefinedType.Short;

      if (tokenType == FSharpTokenType.UINT16)
        return predefinedType.Ushort;

      if (tokenType == FSharpTokenType.INT64)
        return predefinedType.Long;

      if (tokenType == FSharpTokenType.UINT64)
        return predefinedType.Ulong;

      if (tokenType == FSharpTokenType.BYTE)
        return predefinedType.Byte;

      if (tokenType == FSharpTokenType.SBYTE)
        return predefinedType.Sbyte;

      if (tokenType == FSharpTokenType.DECIMAL)
        return predefinedType.Decimal;

      return TypeFactory.CreateUnknownType(psiModule);
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
            return ConstantValue.Create(result, GetPsiModule().GetPredefinedType().Int);
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
