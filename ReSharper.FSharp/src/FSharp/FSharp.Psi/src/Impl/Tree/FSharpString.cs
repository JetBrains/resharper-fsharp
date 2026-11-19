using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
    public class FSharpString : FSharpToken, ILiteralExpression
    {
        public FSharpString(NodeType nodeType, string text) : base(nodeType, text)
        {
        }

        public ConstantValue ConstantValue =>
            ClrConstantValueFactory.CreateStringValue(GetStringValue(), GetPsiModule());

        public ExpressionAccessType GetAccessType() => ExpressionAccessType.Read;
        public ITokenNode Literal => this;

        public IType Type() =>
            LiteralExpr.TryGetLiteralType(GetTokenType(), GetPsiModule()) ??
            GetPsiModule().GetPredefinedType().String;

        public IExpressionType GetExpressionType() => Type();
        public IType GetImplicitlyConvertedTo() => Type();

        public bool IsConstantValue() => FSharpTokenType.ConstantStrings[GetTokenType()];

        private int DollarCount =>
            Parent is IInterpolatedStringExpr interpolatedStringExpr
                ? interpolatedStringExpr.DollarCount
                : FSharpLiteralTypeExtensions.DefaultRawStringDollarCount;

        public (int Start, int End) GetBorderLengths() =>
            GetTokenType().GetBorderLengths(DollarCount);

        [NotNull]
        public FSharpStringLexerBase CreateLexer() =>
            GetTokenType().CreateLexer(new StringBuffer(GetText()), DollarCount);

        [NotNull]
        public string GetStringValue([CanBeNull] RangeTranslator contentRangeTranslator = null)
        {
            var lexer = CreateLexer();
            var value = new StringBuilder();
            var (startBorderLength, endBorderLength) = GetBorderLengths();
            contentRangeTranslator?.StartMapping(GetTextLength() - startBorderLength - endBorderLength);

            lexer.Start();
            while (lexer.TokenType is { } tokenType)
            {
                if (tokenType != StringTokenTypes.START_BORDER_CHARACTER &&
                    tokenType != StringTokenTypes.END_BORDER_CHARACTER)
                {
                    var chars = lexer.CompiledChars;
                    if (tokenType == StringTokenTypes.ESCAPE_CHARACTER)
                        contentRangeTranslator?.MapTextToText(lexer.TokenStart - startBorderLength,
                            lexer.TokenEnd - lexer.TokenStart, value.Length, chars.Length);

                    value.Append(chars);
                }

                lexer.Advance();
            }

            contentRangeTranslator?.EndMapping(value.Length);
            return value.ToString();
        }
    }
}
