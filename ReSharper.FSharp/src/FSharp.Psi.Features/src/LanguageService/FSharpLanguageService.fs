namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService

open FSharp.Compiler.SourceCodeServices
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
open JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CSharp.Impl
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpLanguageService
        (languageType, constantValueService, cacheProvider: FSharpCacheProvider, checkerService: FSharpCheckerService,
         formatter: FSharpDummyCodeFormatter) =
    inherit LanguageService(languageType, constantValueService)

    let lexerFactory = FSharpLexerFactory()

    let getSymbolsCache (psiModule: IPsiModule) =
        if isNull psiModule then null else
        psiModule.GetSolution().GetComponent<IFSharpResolvedSymbolsCache>()

    override x.IsCaseSensitive = true
    override x.SupportTypeMemberCache = true
    override x.CacheProvider = cacheProvider :> _

    override x.GetPrimaryLexerFactory() = lexerFactory :> _
    override x.CreateFilteringLexer(lexer) = lexer

    override x.CreateParser(lexer, _, sourceFile) =
        let psiModule = if isNotNull sourceFile then sourceFile.PsiModule else null
        FSharpParser(lexer, sourceFile, checkerService, getSymbolsCache psiModule) :> _

    member x.CreateParser(document: IDocument, psiModule: IPsiModule) =
        let lexer = TokenBuffer(lexerFactory.CreateLexer(document.Buffer)).CreateLexer()
        FSharpParser(lexer, document, checkerService, getSymbolsCache psiModule) :> IParser

    override x.IsTypeMemberVisible(typeMember) =
        match typeMember with
        | :? IFSharpTypeMember as fsTypeMember -> fsTypeMember.IsVisibleFromFSharp
        | _ -> true

    override x.TypePresenter = CLRTypePresenter.Instance
    override x.DeclaredElementPresenter = CSharpDeclaredElementPresenter.Instance :> _ // todo: implement F# presenter

    override x.CodeFormatter = formatter :> _
    override x.FindTypeDeclarations(_) = EmptyList.Instance :> _

    override x.CanContainCachableDeclarations(node) =
        // workaround for object expressions
        // todo: add API providing additional cachable nodes inside chameleons
        true
//        not (node :? IExpression) || node :? IObjExpr

    override x.CalcOffset(declaration) =
        match declaration with
        | :? IAsPat as asPat -> asPat.Identifier.GetTreeStartOffset()
        | _ -> base.CalcOffset(declaration)

    member x.GetDefaultAccessType(declaredElement: IDeclaredElement) =
        // todo: invocations, partial applications
        match declaredElement with
        | :? IField
        | :? IProperty
        | :? IFSharpAnonRecordFieldProperty ->
            ReferenceAccessType.READ

        | :? IFSharpLocalDeclaration as localDecl ->
            let fsSymbol = localDecl.GetFSharpSymbol()
            if not (fsSymbol :? FSharpMemberOrFunctionOrValue) then ReferenceAccessType.OTHER else

            let mfv = fsSymbol :?> FSharpMemberOrFunctionOrValue
            if not mfv.FullType.IsFunctionType then ReferenceAccessType.READ else 

            ReferenceAccessType.OTHER

        | _ -> ReferenceAccessType.OTHER

    override x.GetReferenceAccessType(declaredElement, reference) =
        match reference.As<FSharpSymbolReference>() with
        | null -> ReferenceAccessType.OTHER
        | symbolReference ->

        match symbolReference.GetElement() with
        | :? IReferenceExpr as referenceExpr ->
            if isNotNull (SetExprNavigator.GetByLeftExpression(referenceExpr.IgnoreParentParens())) then
                ReferenceAccessType.WRITE else

            let indexerExpr = IndexerExprNavigator.GetByExpression(referenceExpr.IgnoreParentParens())
            if isNotNull indexerExpr && isNotNull (SetExprNavigator.GetByLeftExpression(indexerExpr)) then
                ReferenceAccessType.READ else

            x.GetDefaultAccessType(declaredElement)

        | :? IExpressionReferenceName as referenceName ->
            if isNotNull (RecordExprBindingNavigator.GetByReferenceName(referenceName)) then
                ReferenceAccessType.WRITE else

            x.GetDefaultAccessType(declaredElement)

        | _ ->
            x.GetDefaultAccessType(declaredElement)

    override x.CreateElementPointer(declaredElement) =
        match declaredElement.As<IFSharpGeneratedFromOtherElement>() with
        | null -> null
        | generatedElement -> generatedElement.CreatePointer() :?> _

    override x.AnalyzePossibleInfiniteInheritance = false

    interface IFSharpLanguageService with
        member x.CreateParser(document: IDocument) =
            let lexer = TokenBuffer(lexerFactory.CreateLexer(document.Buffer)).CreateLexer()
            FSharpParser(lexer, document, checkerService, null) :> _
        
        member x.CreateElementFactory(psiModule) = FSharpElementFactory(x, psiModule) :> _
