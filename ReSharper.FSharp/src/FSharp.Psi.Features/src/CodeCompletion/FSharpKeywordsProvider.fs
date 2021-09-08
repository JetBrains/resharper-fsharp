namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open System.Collections.Generic
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Syntax
open FSharp.Compiler.Tokenization
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.RdBackend.Common.Features.Completion
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Psi.Tree
open JetBrains.TextControl
open JetBrains.UI.RichText
open JetBrains.Util

[<RequireQualifiedAccess>]
type KeywordSuffix =
    | Quotes
    | Space
    | None


module FSharpKeywordsProvider =
    let reparseContextAwareKeywords =
        [| "abstract"
           "and!"
           "default"
           "do!"
           "exception"
           "extern"
           "let!"
           "match!"
           "member"
           "module"
           "namespace"
           "open"
           "override"
           "return!"
           "type"
           "static"
           "use!"
           "val"
           "yield!" |]
        |> HashSet

    let alwaysSuggestedKeywords =
        FSharpKeywords.KeywordsWithDescription
        |> List.filter (fun (keyword, _) ->
            not (reparseContextAwareKeywords.Contains(keyword)) &&
            not (PrettyNaming.IsOperatorName keyword))

    let keywordDescriptions =
        dict FSharpKeywords.KeywordsWithDescription

    let isInComputationExpression (context: FSharpCodeCompletionContext) =
        let reference = context.ReparsedContext.Reference
        if isNull reference then false, false else

        let refExpr = reference.GetTreeNode().As<IReferenceExpr>()
        if isNull refExpr then false, false else

        let rec loop isLetInExpr (expr: IFSharpExpression) =
            if isNotNull (ComputationExprNavigator.GetByExpression(expr)) then true, isLetInExpr else

            let letOrUseExpr = LetOrUseExprNavigator.GetByInExpression(expr)
            if isNotNull letOrUseExpr then loop true letOrUseExpr else

            let seqExpr = SequentialExprNavigator.GetByExpression(expr)
            if isNotNull seqExpr then loop isLetInExpr seqExpr else

            let matchExpr = MatchExprNavigator.GetByClauseExpression(expr)
            if isNotNull matchExpr then loop isLetInExpr matchExpr else

            let ifExpr = IfExprNavigator.GetByBranchExpression(expr)
            if isNotNull ifExpr then loop isLetInExpr ifExpr else

            let whileExpr = WhileExprNavigator.GetByDoExpression(expr)
            if isNotNull whileExpr then loop isLetInExpr whileExpr else

            let forExpr = ForExprNavigator.GetByDoExpression(expr)
            if isNotNull forExpr then loop isLetInExpr forExpr else

            let tryExpr = TryLikeExprNavigator.GetByTryExpression(expr)
            if isNotNull tryExpr then loop isLetInExpr tryExpr else

            let prefixAppExpr = PrefixAppExprNavigator.GetByFunctionExpression(expr)
            if isNotNull prefixAppExpr then loop isLetInExpr prefixAppExpr else

            let binaryAppExpr = BinaryAppExprNavigator.GetByLeftArgument(expr)
            if isNotNull binaryAppExpr then loop isLetInExpr binaryAppExpr else

            false, false

        loop false refExpr

    let isModuleMemberStart (context: FSharpCodeCompletionContext) =
        let reference = context.ReparsedContext.Reference
        if isNull reference then false, null else

        let treeNode = reference.GetTreeNode()
        match treeNode with
        | :? ITypeReferenceName as referenceName ->
            let moduleAbbreviationDecl = ModuleAbbreviationDeclarationNavigator.GetByTypeName(referenceName)
            isNotNull moduleAbbreviationDecl, moduleAbbreviationDecl :> ITreeNode

        | :? IReferenceExpr as refExpr ->
            let rec loop (expr: IFSharpExpression) =
                match PrefixAppExprNavigator.GetByFunctionExpression(expr) with
                | null -> expr
                | prefixAppExpr -> loop prefixAppExpr

            let expr = loop refExpr
            let doStmt = ExpressionStatementNavigator.GetByExpression(expr)
            let moduleDecl = ModuleLikeDeclarationNavigator.GetByMember(doStmt)
            isNotNull moduleDecl, moduleDecl :> _

        | _ -> false, null

    let isAtTypeInOpen (context: FSharpCodeCompletionContext) =
        let reference = context.ReparsedContext.Reference
        if isNull reference then false else

        let referenceName = reference.GetTreeNode().As<ITypeReferenceName>()
        if isNull referenceName || isNotNull referenceName.Qualifier then false else

        let rec loop (referenceName: ITypeReferenceName) =
            let qualifiedReferenceName = TypeReferenceNameNavigator.GetByQualifier(referenceName)
            if isNotNull qualifiedReferenceName then loop qualifiedReferenceName else
            isNotNull (OpenStatementNavigator.GetByReferenceName(referenceName))

        loop referenceName

    let mayStartTypeMember (context: FSharpCodeCompletionContext) =
        let reference = context.ReparsedContext.Reference
        if isNull reference then true else

        // todo: get element from the context
        match reference.GetTreeNode() with
        | :? IReferenceExpr ->
            false

        | :? ITypeReferenceName as referenceName ->
            let typeUsage = NamedTypeUsageNavigator.GetByReferenceName(referenceName)
            let declaration = TypeUsageOrUnionCaseDeclarationNavigator.GetByTypeUsage(typeUsage)
            isNotNull declaration

        | _ -> true
    
    let suggestKeywords (context: FSharpCodeCompletionContext) = seq {
        let isModuleMemberStart, moduleDecl = isModuleMemberStart context
        if isModuleMemberStart then
            "exception"
            "extern"
            "open"
            "module"
            "type" // todo: visibility before type recovery

        if moduleDecl :? INamespaceDeclaration || moduleDecl :? IAnonModuleDeclaration then
            "namespace"

        if isAtTypeInOpen context then
            "type"

        let inComputationExpression, isLetInExpr = isInComputationExpression context
        if inComputationExpression then
            "do!"
            "let!"
            "match!"
            "return!"
            "use!"
            "yield!"
            
            if isLetInExpr then
                "and!"

        if mayStartTypeMember context then
            "abstract"
            "default"
            "member"
            "override"
            "static"
            "val"
    }

type FSharpKeywordLookupItemBase(keyword, keywordSuffix: KeywordSuffix) =
    inherit TextLookupItemBase()

    override x.Image = PsiSymbolsThemedIcons.Keyword.Id

    override x.Text =
        match keywordSuffix with
        | KeywordSuffix.Space -> $"{keyword} "
        | KeywordSuffix.Quotes -> $"{keyword} \"\""
        | _ -> keyword

    override x.GetDisplayName() =
        LookupUtil.FormatLookupString(keyword, x.TextColor)

    override x.Accept(textControl, nameRange, insertType, suffix, solution, keepCaret) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaret)

        match keywordSuffix with
        | KeywordSuffix.Quotes ->
            // Move caret back inside inserted quotes.
            textControl.Caret.MoveTo(textControl.Caret.Offset() - 1, CaretVisualPlacement.DontScrollIfVisible)
            textControl.RescheduleCompletion(solution)
        | _ -> ()

    interface IRiderAsyncCompletionLookupItem


type FSharpKeywordLookupItem(keyword, description: string, isReparseContextAware) =
    inherit FSharpKeywordLookupItemBase(keyword, KeywordSuffix.None)

    member val IsReparseContextAware = isReparseContextAware
    
    interface IDescriptionProvidingLookupItem with
        member x.GetDescription() = RichTextBlock(description)


type FSharpHashDirectiveLookupItem(directive, suffix) =
    inherit FSharpKeywordLookupItemBase(directive, suffix)


[<Language(typeof<FSharpLanguage>)>]
type FSharpKeywordsProvider() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let hashDirectives =
        [| KeywordSuffix.Quotes, [| "#load"; "#r"; "#I"; "#nowarn"; "#time" |]
           KeywordSuffix.None, [| "#if"; "#else"; "#endif" |] |]
        |> Array.map (fun (suffix, directives) -> directives |> Array.map (fun d -> d, suffix))
        |> Array.concat

    let scriptKeywords =
        [| "__SOURCE_DIRECTORY__"
           "__SOURCE_FILE__"
           "__LINE__" |]

    override x.IsAvailable _ = true
    override x.GetDefaultRanges(context) = context.Ranges
    override x.GetLookupFocusBehaviour _ = LookupFocusBehaviour.Soft

    override x.AddLookupItems(context, collector) =
        let reparsedContext = context.ReparsedContext
        let reference = reparsedContext.Reference.As<FSharpSymbolReference>()
        if isNotNull reference && reference.IsQualified then false else

        let tokenBeforeCaret = context.TokenBeforeCaret
        let fcsCompletionContext = reparsedContext.GetFcsContext()

        match fcsCompletionContext.CompletionContext, getTokenType tokenBeforeCaret with
        | Some(CompletionContext.Invalid), tokenBeforeType when tokenBeforeType != FSharpTokenType.HASH -> false
        | _, tokenBeforeType ->

        if tokenBeforeType == FSharpTokenType.LINE_COMMENT ||
           tokenBeforeType == FSharpTokenType.DEAD_CODE ||
           tokenBeforeType == FSharpTokenType.DOT ||
           tokenBeforeType == FSharpTokenType.RESERVED_LITERAL_FORMATS ||
           isNotNull tokenBeforeType && tokenBeforeType.IsConstantLiteral ||
           tokenBeforeCaret == context.TokenAtCaret && isNotNull tokenBeforeType &&
               (tokenBeforeType.IsComment || tokenBeforeType.IsStringLiteral || tokenBeforeType.IsConstantLiteral)
        then false else

        if not fcsCompletionContext.PartialName.QualifyingIdents.IsEmpty then false else

        let add isReparseContext keywords =
            for keyword, description in keywords do
                let item = FSharpKeywordLookupItem(keyword, description, isReparseContext)
                item.InitializeRanges(context.Ranges, context.BasicContext)
                markRelevance item CLRLookupItemRelevance.Keywords

                match keyword with
                | "true" | "false" | "null" ->
                    // use the same relevance as module members
                    // todo: add F#-specific relevance
                    markRelevance item CLRLookupItemRelevance.Methods
                | _ -> ()

                collector.Add(item)
            ()

        add false FSharpKeywordsProvider.alwaysSuggestedKeywords
        add true (FSharpKeywordsProvider.suggestKeywords context |> Seq.map (fun k -> k, ""))

        if context.BasicContext.File.Language.Is<FSharpScriptLanguage>() then
            for keyword in scriptKeywords do
                let item = FSharpKeywordLookupItem(keyword, "", false)
                item.InitializeRanges(context.Ranges, context.BasicContext)
                collector.Add(item)

        for keyword, suffix in hashDirectives do
            let item = FSharpHashDirectiveLookupItem(keyword, suffix)
            item.InitializeRanges(context.Ranges, context.BasicContext)
            collector.Add(item)

        true


[<SolutionComponent>]
type FSharpHashDirectiveAutocompletionStrategy() =
    interface IAutomaticCodeCompletionStrategy with
        member x.Language = FSharpLanguage.Instance :> _

        member x.AcceptsFile(file, _) =
            match file.GetSourceFile() with
            | null -> false
            | sourceFile -> sourceFile.LanguageType.Is<FSharpScriptProjectFileType>()

        member x.AcceptTyping(char, _, _) = char = '#'
        member x.ProcessSubsequentTyping(char, _) = char.IsLetterFast()

        member x.IsEnabledInSettings(_, _) = AutopopupType.SoftAutopopup
        member x.ForceHideCompletion = false
