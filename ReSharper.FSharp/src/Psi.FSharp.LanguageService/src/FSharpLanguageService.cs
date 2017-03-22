using System.Collections.Generic;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.LanguageService
{
  [Language(typeof(FSharpLanguage))]
  public class FSharpLanguageService : Psi.LanguageService
  {
    private readonly FSharpCheckerService myFSharpCheckerService;

    public FSharpLanguageService(PsiLanguageType psiLanguageType, IConstantValueService constantValueService,
      FSharpCheckerService fSharpCheckerService) : base(psiLanguageType, constantValueService)
    {
      myFSharpCheckerService = fSharpCheckerService;
      CacheProvider = new FSharpCacheProvider();
    }

    public override ILexerFactory GetPrimaryLexerFactory()
    {
      return new FSharpFakeLexerFactory();
    }

    public override ILexer CreateFilteringLexer(ILexer lexer)
    {
      return lexer;
    }

    public override bool IsTypeMemberVisible(ITypeMember member)
    {
      return true;
    }

    public override IParser CreateParser(ILexer lexer, IPsiModule module, IPsiSourceFile sourceFile)
    {
      return new FSharpParser(sourceFile, myFSharpCheckerService);
    }

    public override IDeclaredElementPresenter DeclaredElementPresenter =>
      CSharpDeclaredElementPresenter.Instance; // todo: replace with F#-specific presenter

    public override IEnumerable<ITypeDeclaration> FindTypeDeclarations(IFile file)
    {
      return EmptyList<ITypeDeclaration>.Instance;
    }

    public override ILanguageCacheProvider CacheProvider { get; }
    public override bool IsCaseSensitive => true;
    public override bool SupportTypeMemberCache => true;
    public override ITypePresenter TypePresenter => CLRTypePresenter.Instance;
  }
}