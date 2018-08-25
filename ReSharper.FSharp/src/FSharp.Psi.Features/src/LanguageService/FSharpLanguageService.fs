namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
open JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CSharp.Impl
open JetBrains.ReSharper.Psi.Impl
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpLanguageService(languageType, constantValueService, formatter: FSharpDummyCodeFormatter, fsCheckerService, logger) =
    inherit LanguageService(languageType, constantValueService)

    static let fakeLexerFactory = FSharpFakeLexerFactory()

    let cacheProvider = FSharpCacheProvider(fsCheckerService)

    override x.IsCaseSensitive = true
    override x.SupportTypeMemberCache = true
    override x.CacheProvider = cacheProvider :> _

    override x.GetPrimaryLexerFactory() = fakeLexerFactory :> _
    override x.CreateFilteringLexer(lexer) = lexer
    override x.CreateParser(lexer, psiModule, sourceFile) = FSharpParser(sourceFile, fsCheckerService, logger) :> _

    override x.IsTypeMemberVisible(typeMember) =
        match typeMember with
        | :? IFSharpTypeMember as fsTypeMember -> fsTypeMember.IsVisibleFromFSharp
        | _ -> true

    override x.TypePresenter = CLRTypePresenter.Instance
    override x.DeclaredElementPresenter = CSharpDeclaredElementPresenter.Instance :> _ // todo: implement F# presenter

    override x.CodeFormatter = formatter :> _
    override x.FindTypeDeclarations(file) = EmptyList<_>.Instance :> _


[<Language(typeof<FSharpLanguage>)>]
type FSharpScriptLanguageService(languageType, constantValueService, formatter, fsCheckerService, logger) =
    inherit FSharpLanguageService(languageType, constantValueService, formatter, fsCheckerService, logger)
