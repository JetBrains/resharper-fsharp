using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Util.Literals;
using JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.RegExp.ClrRegex;
using JetBrains.ReSharper.Psi.RegExp.ClrRegex.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Cs.Injected
{
  [SolutionComponent]
  public class FSharpLiteralInjectionTarget : IInjectionTargetLanguage
  {
    public bool ShouldInjectByAnnotation(ITreeNode originalNode, out string prefix, out string postfix)
    {
      prefix = null;
      postfix = null;
      return false;
    }

    public int GetStartOffsetForString(ITreeNode originalNode)
    {
      return (originalNode as FSharpToken)?.GetText()[0] == '@' ? 2 : 1;
    }

    public int GetEndOffsetForString(ITreeNode originalNode)
    {
      return 1;
    }

    public ITreeNode UpdateNode(IFile generatedFile, ITreeNode generatedNode, ITreeNode originalNode, out int length,
      string prefix, string postfix, int startOffset, int endOffset)
    {
      length = -1;
      return null;
    }

    bool IInjectionTargetLanguage.SupportsRegeneration => false;

    public bool IsInjectionAllowed(ITreeNode literalNode)
    {
      return literalNode.GetTokenType()?.IsStringLiteral ?? false;
    }

    public string GetCorrespondingCommentTextForLiteral(ITreeNode originalNode)
    {
      return null;
    }

    public IBuffer CreateBuffer(ITreeNode originalNode, string text, object options)
    {
      var literalType = text.StartsWith("@", StringComparison.Ordinal)
        ? CSharpLiteralType.VerbatimString
        : CSharpLiteralType.RegularString;
      return new CSharpRegExpBuffer(new StringBuffer(text), literalType, ClrRegexLexerOptions.None);
    }

    public bool DoNotProcessNodeInterior(ITreeNode element)
    {
      return false;
    }

    public bool IsPrimaryLanguageApplicable(IPsiSourceFile sourceFile)
    {
      return sourceFile.PrimaryPsiLanguage.Is<FSharpLanguage>();
    }

    public ILexerFactory CreateLexerFactory(LanguageService languageService)
    {
      return languageService.GetPrimaryLexerFactory();
    }

    public bool AllowsLineBreaks(ITreeNode originalNode)
    {
      return false;
    }

    public bool IsWhitespaceToken(ITokenNode token)
    {
      return token.GetTokenType().IsWhitespace;
    }

    public TreeTextRange FixValueRangeForLiteral(ITreeNode element)
    {
      return element.GetTreeTextRange().TrimLeft(1).TrimRight(1);
    }

    public PsiLanguageType Language => FSharpLanguage.Instance;
  }
}
