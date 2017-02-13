using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.TreeBuilder;
using JetBrains.Text;

namespace JetBrains.ReSharper.Psi.FSharp.LanguageService
{
  public class FSharpPsiBuilderTokenFactory : IPsiBuilderTokenFactory
  {
    public LeafElementBase CreateToken(TokenNodeType tokenNodeType, IBuffer buffer, int startOffset, int endOffset)
    {
      return tokenNodeType == FSharpTokenType.IDENTIFIER || tokenNodeType == FSharpTokenType.OPERATOR
        ? new FSharpIdentifierToken(tokenNodeType, buffer, new TreeOffset(startOffset), new TreeOffset(endOffset))
        : tokenNodeType.Create(buffer, new TreeOffset(startOffset), new TreeOffset(endOffset));
    }
  }
}