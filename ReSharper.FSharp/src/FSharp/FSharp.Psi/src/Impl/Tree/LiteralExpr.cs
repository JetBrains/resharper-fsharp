using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Injections;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LiteralExpr
  {
    private static readonly NodeTypeSet ourConstantValueTokenTypes =
      new(
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

    private static readonly NodeTypeDictionary<IClrTypeName> ourLiteralTypeNames =
      new(new List<KeyValuePair<NodeType, IClrTypeName>>
      {
        new(FSharpTokenType.TRUE, PredefinedType.BOOLEAN_FQN),
        new(FSharpTokenType.FALSE, PredefinedType.BOOLEAN_FQN),
        new(FSharpTokenType.SBYTE, PredefinedType.SBYTE_FQN),
        new(FSharpTokenType.BYTE, PredefinedType.BYTE_FQN),
        new(FSharpTokenType.INT16, PredefinedType.SHORT_FQN),
        new(FSharpTokenType.UINT16, PredefinedType.USHORT_FQN),
        new(FSharpTokenType.INT32, PredefinedType.INT_FQN),
        new(FSharpTokenType.UINT32, PredefinedType.UINT_FQN),
        new(FSharpTokenType.INT64, PredefinedType.LONG_FQN),
        new(FSharpTokenType.UINT64, PredefinedType.ULONG_FQN),
        new(FSharpTokenType.NATIVEINT, PredefinedType.INTPTR_FQN),
        new(FSharpTokenType.UNATIVEINT, PredefinedType.UINTPTR_FQN),
        new(FSharpTokenType.IEEE32, PredefinedType.FLOAT_FQN),
        new(FSharpTokenType.IEEE64, PredefinedType.DOUBLE_FQN),
        new(FSharpTokenType.DECIMAL, PredefinedType.DECIMAL_FQN),
        new(FSharpTokenType.CHARACTER_LITERAL, PredefinedType.CHAR_FQN),
        new(FSharpTokenType.BYTECHAR, PredefinedType.BYTE_FQN),
        new(FSharpTokenType.STRING, PredefinedType.STRING_FQN),
        new(FSharpTokenType.VERBATIM_STRING, PredefinedType.STRING_FQN),
        new(FSharpTokenType.TRIPLE_QUOTED_STRING, PredefinedType.STRING_FQN),
      });

    public override bool IsConstantValue()
    {
      var tokenType = Literal?.GetTokenType();
      return ourConstantValueTokenTypes[tokenType];
    }

    public override IType Type() =>
      TryGetLiteralType(Literal?.GetTokenType(), GetPsiModule()) ?? base.Type();

    [CanBeNull]
    internal static IType TryGetLiteralType([CanBeNull] TokenNodeType tokenType, [NotNull] IPsiModule psiModule)
    {
      var predefinedType = psiModule.GetPredefinedType();
      if (tokenType == FSharpTokenType.BYTEARRAY || tokenType == FSharpTokenType.VERBATIM_BYTEARRAY)
        return TypeFactory.CreateArrayType(predefinedType.Byte, 1, NullableAnnotation.Unknown);

      return ourLiteralTypeNames[tokenType] is { } typeName
        ? predefinedType.TryGetType(typeName, NullableAnnotation.Unknown)
        : null;
    }

    public override ConstantValue ConstantValue
    {
      get
      {
        var literal = Literal;
        if (literal == null)
          return ConstantValue.BAD_VALUE;

        if (literal is FSharpString stringToken)
          return stringToken.IsConstantValue() ? stringToken.ConstantValue : ConstantValue.NOT_COMPILE_TIME_CONSTANT;

        var tokenType = literal.GetTokenType();
        if (tokenType == FSharpTokenType.INT32)
        {
          var text = literal.GetText().TrimEnd('l');
          var literalBase = GetIntegerBase(text);
          var startOffset = literalBase == IntBase.Decimal ? 0 : 2;
          var literalText = text.Substring(startOffset).Replace("_", string.Empty);

          try
          {
            var isNegative = FirstChild?.GetTokenType() == FSharpTokenType.MINUS;
            var result = ParseInt32(literalText, literalBase, isNegative);
            return ConstantValue.Create(result, GetPsiModule().GetPredefinedType().Int);
          }
          catch (Exception)
          {
            return ConstantValue.BAD_VALUE;
          }
        }

        return ConstantValue.NOT_COMPILE_TIME_CONSTANT;
      }
    }

    private static int ParseInt32(string digits, IntBase literalBase, bool isNegative)
    {
      if (literalBase == IntBase.Decimal)
        return Convert.ToInt32(isNegative ? "-" + digits : digits, 10);

      var value = Convert.ToInt32(digits, (int)literalBase);
      return isNegative ? unchecked(-value) : value;
    }

    private static IntBase GetIntegerBase(string literalText)
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

    bool IInjectionHostNode.IsValidHost =>
      FSharpTokenType.ConstantStrings[Literal?.GetTokenType()];
  }
}
