using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
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
    public FSharpLanguageService(PsiLanguageType psiLanguageType, IConstantValueService constantValueService)
      : base(psiLanguageType, constantValueService)
    {
    }

    public override ILexerFactory GetPrimaryLexerFactory()
    {
      return new FSharpFakeLexerFactory();
    }

    public override ILexer CreateFilteringLexer(ILexer lexer)
    {
      return lexer;
    }

    public override IParser CreateParser(ILexer lexer, IPsiModule module, IPsiSourceFile sourceFile)
    {
      return new FSharpFakeParser(sourceFile);
    }

    public override IEnumerable<ITypeDeclaration> FindTypeDeclarations(IFile file)
    {
      return EmptyList<ITypeDeclaration>.Instance;
    }

    public override ILanguageCacheProvider CacheProvider => null;
    public override bool IsCaseSensitive => true;
    public override bool SupportTypeMemberCache => true;
    public override ITypePresenter TypePresenter => CLRTypePresenter.Instance;
  }
}