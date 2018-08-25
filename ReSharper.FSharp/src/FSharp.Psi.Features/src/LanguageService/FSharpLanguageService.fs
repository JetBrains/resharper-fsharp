namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService

open System
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
open JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CSharp.Impl
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Naming.Impl
open JetBrains.Util
open Microsoft.FSharp.Compiler

[<Language(typeof<FSharpLanguage>)>]
type FSharpLanguageService
        (languageType, constantValueService, cacheProvider: FSharpCacheProvider, formatter: FSharpDummyCodeFormatter,
         fsCheckerService: FSharpCheckerService, namingService: FSharpNamingService, logger: ILogger) =
    inherit LanguageService(languageType, constantValueService)

    static let fakeLexerFactory = FSharpFakeLexerFactory()

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

    override x.IsValidName(elementType, name) =
        namingService.IsValidName(elementType, name)


[<Language(typeof<FSharpLanguage>)>]
type FSharpNamingService(language: FSharpLanguage) =
    inherit NamingLanguageServiceBase(language)

    static let notAllowedInTypes =
        [| '.'; '+'; '$'; '&'; '['; ']'; '/'; '\\'; '*'; '\"'; '`' |]

    override x.MangleNameIfNecessary(name, _) =
        Lexhelp.Keywords.QuoteIdentifierIfNeeded name

    member x.IsValidName(elementType: DeclaredElementType, name: string) =
        if name.IsEmpty() then false else

        if elementType == FSharpDeclaredElementType.UnionCase &&
                (Char.IsLower(name.[0]) || not (Char.IsUpper(name.[0]))) then
            false else

        if (elementType == CLRDeclaredElementType.CLASS || elementType == CLRDeclaredElementType.STRUCT ||
                elementType == CLRDeclaredElementType.INTERFACE || elementType == CLRDeclaredElementType.NAMESPACE ||
                elementType == FSharpDeclaredElementType.UnionCase) &&
                name.IndexOfAny(notAllowedInTypes) <> -1 then
            false else

        not (startsWith "`" name || endsWith "`" name || name.ContainsNewLine() || name.Contains("``"))
