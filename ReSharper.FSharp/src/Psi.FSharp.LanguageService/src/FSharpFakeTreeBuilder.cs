using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.TreeBuilder;

namespace JetBrains.ReSharper.Psi.FSharp.LanguageService
{
  public class FSharpFakeTreeBuilder : TreeStructureBuilderBase
  {
    public FSharpFakeTreeBuilder([NotNull] ILexer lexer, Lifetime lifetime) : base(lifetime)
    {
      var tokenFactory = new FSharpPsiBuilderTokenFactory();
      Builder = new PsiBuilder(lexer, ElementType.F_SHARP_FILE, tokenFactory, lifetime);
    }

    public CompositeElement CreateFakeFile()
    {
      var fileMark = Builder.Mark();
      while (Builder.GetTokenType() != null)
        Builder.AdvanceLexer();
      Done(fileMark, ElementType.F_SHARP_FILE);
      return GetTree();
    }

    protected override string GetExpectedMessage(string name)
    {
      throw new System.NotImplementedException();
    }

    protected override PsiBuilder Builder { get; }
    protected override TokenNodeType NewLine => FSharpTokenType.NEW_LINE;
    protected override NodeTypeSet CommentsOrWhiteSpacesTokens => FSharpTokenType.CommentsOrWhitespaces;
  }
}