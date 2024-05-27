namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open System.Runtime.InteropServices
open FSharp.Compiler.Symbols
open JetBrains.Application.Parts
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.CodeFormatter
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
open JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CSharp.Impl
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpLanguageService(languageType, constantValueService, cacheProvider: FSharpCacheProvider,
        checkerService: FcsCheckerService, formatter: FSharpCodeFormatter) =
    inherit LanguageService(languageType, constantValueService)

    let lexerFactory = FSharpLexerFactory()

    let getSymbolsCache (psiModule: IPsiModule) =
        if isNull psiModule then null else
        psiModule.GetSolution().GetComponent<IFcsResolvedSymbolsCache>()

    override x.IsCaseSensitive = true
    override x.SupportTypeMemberCache = true
    override x.CacheProvider = cacheProvider :> _

    override x.GetPrimaryLexerFactory() = lexerFactory :> _
    override x.CreateFilteringLexer(lexer) = lexer

    override x.CreateParser(lexer, _, sourceFile) =
        let psiModule = if isNotNull sourceFile then sourceFile.PsiModule else null
        FSharpParser(lexer, sourceFile, checkerService, getSymbolsCache psiModule) :> _

    override x.IsTypeMemberVisible(typeMember) =
        match typeMember with
        | :? IFSharpTypeMember as fsTypeMember -> fsTypeMember.IsVisibleFromFSharp
        | _ -> true

    override x.TypePresenter = CLRTypePresenter.Instance
    override x.DeclaredElementPresenter = CSharpDeclaredElementPresenter.Instance :> _ // todo: implement F# presenter

    override x.CodeFormatter = formatter :> _
    override x.FindTypeDeclarations _ = EmptyList.Instance :> _

    override x.CanContainCachableDeclarations(node) =
        not (node :? IExpression || node :? IChameleonExpression) || node :? IObjExpr

    override x.GetAdditionalCachableDeclarations(file) =
        let fsFile = file.As<IFSharpFile>()
        let sourceFile = fsFile.GetSourceFile()
        FSharpCacheDeclarationProcessor.GetObjectExpressions(fsFile, sourceFile) |> Seq.cast

    member x.GetDefaultAccessType(declaredElement: IDeclaredElement) =
        // todo: invocations, partial applications
        match declaredElement with
        | :? IUnionCase ->
            ReferenceAccessType.OTHER

        | :? IField
        | :? IProperty
        | :? IFSharpAnonRecordFieldProperty ->
            ReferenceAccessType.READ

        | :? IFSharpLocalDeclaration as localDecl ->
            let fcsSymbol = localDecl.GetFcsSymbol()
            if not (fcsSymbol :? FSharpMemberOrFunctionOrValue) then ReferenceAccessType.OTHER else

            let mfv = fcsSymbol :?> FSharpMemberOrFunctionOrValue
            if not mfv.FullType.IsFunctionType then ReferenceAccessType.READ else

            ReferenceAccessType.OTHER

        | _ -> ReferenceAccessType.OTHER

    override x.GetReferenceAccessType(declaredElement, reference) =
        match reference.As<FSharpSymbolReference>() with
        | null -> ReferenceAccessType.OTHER
        | symbolReference ->

        match symbolReference.GetElement() with
        | :? IReferenceExpr as referenceExpr ->
            let refExprOrIndexerLikeExpr = getIndexerExprOrIgnoreParens referenceExpr
            if isNotNull (SetExprNavigator.GetByLeftExpression(refExprOrIndexerLikeExpr)) then
                ReferenceAccessType.WRITE else

            let isInstanceFieldOrProperty (element: IDeclaredElement) =
                match element with
                | :? IField as field -> not field.IsStatic && not (field :? IFSharpPatternDeclaredElement)
                | :? IProperty as property -> not property.IsStatic
                | _ -> false

            let indexerExpr = IndexerExprNavigator.GetByQualifier(referenceExpr.IgnoreParentParens())
            if isNotNull indexerExpr && isNotNull (SetExprNavigator.GetByLeftExpression(indexerExpr)) then
                ReferenceAccessType.READ else

            let isNamedArg () =
                let binaryAppExpr = BinaryAppExprNavigator.GetByLeftArgument(referenceExpr)
                FSharpMethodInvocationUtil.isNamedArgSyntactically binaryAppExpr

            if isInstanceFieldOrProperty declaredElement && isNamedArg () then
                ReferenceAccessType.WRITE else

            x.GetDefaultAccessType(declaredElement)

        | :? IExpressionReferenceName as referenceName ->
            if isNotNull (RecordFieldBindingNavigator.GetByReferenceName(referenceName)) then
                ReferenceAccessType.WRITE else

            if isNotNull (ReferencePatNavigator.GetByReferenceName(referenceName)) ||
                   isNotNull (AnonRecordFieldNavigator.GetByReferenceName(referenceName)) ||
                   isNotNull (ParametersOwnerPatNavigator.GetByReferenceName(referenceName)) ||
                   isNotNull (FieldPatNavigator.GetByReferenceName(referenceName)) then
                ReferenceAccessType.OTHER else

            x.GetDefaultAccessType(declaredElement)

        | _ ->
            x.GetDefaultAccessType(declaredElement)

    override x.CreateElementPointer(declaredElement) =
        match declaredElement.As<IFSharpGeneratedFromOtherElement>() with
        | null -> null
        | generatedElement -> generatedElement.CreatePointer() :?> _

    override x.AnalyzeFormatStrings = false
    override x.AnalyzePossibleInfiniteInheritance = false

    override x.GetTypeConversionRule(_, _) = ClrPredefinedTypeConversionRule.INSTANCE

    interface IFSharpLanguageService with
        member x.CreateParser(document: IDocument, sourceFile, [<Optional; DefaultParameterValue(null)>] overrideExtension) =
            let lexer = TokenBuffer(lexerFactory.CreateLexer(document.Buffer)).CreateLexer()
            FSharpParser(lexer, document, sourceFile, checkerService, null, overrideExtension) :> _

        member x.CreateElementFactory(sourceFile, psiModule, [<Optional; DefaultParameterValue(null)>] extension) =
            FSharpElementFactory(x, sourceFile, psiModule, extension) :> _
