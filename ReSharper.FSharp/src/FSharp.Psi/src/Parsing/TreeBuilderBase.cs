using JetBrains.DataFlow;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.TreeBuilder;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
    public class TreeBuilderBase : TreeStructureBuilderBase
    {
        public TreeBuilderBase(Lifetime lifetime, ILexer lexer) : base(lifetime)
        {
            var tokenFactory = new TokenFactory(IdentifierIntern);
            Builder = new PsiBuilder(lexer, ElementType.F_SHARP_IMPL_FILE, tokenFactory, lifetime);
        }

        protected override PsiBuilder Builder { get; }

        protected override TokenNodeType NewLine => FSharpTokenType.NEW_LINE;
        protected override NodeTypeSet CommentsOrWhiteSpacesTokens => FSharpTokenType.AccessModifiersKeywords;

        protected override string GetExpectedMessage(string name) =>
            throw new System.NotImplementedException();

        private class TokenFactory : IPsiBuilderTokenFactory
        {
            private readonly ITokenIntern myIdentifierIntern;

            public TokenFactory(ITokenIntern identifierIntern) =>
                myIdentifierIntern = identifierIntern;

            public LeafElementBase CreateToken(TokenNodeType tokenType, IBuffer buffer, int startOffset, int endOffset)
            {
                if (tokenType is IFixedTokenNodeType)
                    return tokenType.Create(null);

                var text =
                    FSharpTokenType.Identifiers[tokenType]
                        ? myIdentifierIntern.Intern(buffer, startOffset, endOffset)
                        : buffer.GetText(new TextRange(startOffset, endOffset));
                return tokenType.Create(text);
            }
        }
    }
}