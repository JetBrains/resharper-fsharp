using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.TreeBuilder;
using JetBrains.Text;

namespace JetBrains.ReSharper.Psi.FSharp.LanguageService
{
  public class FSharpPsiBuilderTokenFactory : IPsiBuilderTokenFactory
  {
    public LeafElementBase CreateToken(TokenNodeType tokenNodeType, IBuffer buffer, int startOffset, int endOffset)
    {
      return tokenNodeType.Create(buffer, new TreeOffset(startOffset), new TreeOffset(endOffset));
    }
  }
}