using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Services.Formatter;
using JetBrains.ReSharper.Psi.CodeStyle;
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
    private readonly ILogger myLogger;

    public FSharpLanguageService(PsiLanguageType psiLanguageType, IConstantValueService constantValueService,
      FSharpDummyCodeFormatter codeFormatter, FSharpCheckerService fSharpCheckerService, ILogger logger)
      : base(psiLanguageType, constantValueService)
    {
      CodeFormatter = codeFormatter;
      myFSharpCheckerService = fSharpCheckerService;
      myLogger = logger;
      CacheProvider = new FSharpCacheProvider(fSharpCheckerService, myLogger);
    }

    public override ICodeFormatter CodeFormatter { get; }

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
      return (member as IFSharpTypeMember)?.IsVisibleFromFSharp ?? true;
    }

    public override IParser CreateParser(ILexer lexer, IPsiModule module, IPsiSourceFile sourceFile)
    {
      return new FSharpParser(sourceFile, myFSharpCheckerService, myLogger);
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