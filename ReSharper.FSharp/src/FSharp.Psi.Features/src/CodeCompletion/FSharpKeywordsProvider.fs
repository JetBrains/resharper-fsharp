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
    let computationExpressionKeywords = 
        [| "and!"
           "do!"
           "let!"
           "match!"
           "return!"
           "use!"
           "yield!" |]
        |> HashSet

    let reparseContextAwareKeywords =
        HashSet(computationExpressionKeywords)

    let alwaysSuggestedKeywords =
        FSharpKeywords.KeywordsWithDescription
        |> List.filter (fun (keyword, _) ->
            not (reparseContextAwareKeywords.Contains(keyword)) &&
            not (PrettyNaming.IsOperatorName keyword))

    let keywordDescriptions =
        dict FSharpKeywords.KeywordsWithDescription

    let isInComputationExpression (context: FSharpCodeCompletionContext) =
        let reference = context.ReparsedContext.Reference
        if isNull reference then false else

        let refExpr = reference.GetTreeNode().As<IReferenceExpr>()
        if isNull refExpr then false else

        let rec loop (expr: IFSharpExpression) =
            if expr :? IComputationExpr then true else

            let letOrUseExpr = LetOrUseExprNavigator.GetByInExpression(refExpr)
            if isNotNull letOrUseExpr then loop letOrUseExpr else

            let seqExpr = SequentialExprNavigator.GetByExpression(refExpr)
            if isNotNull seqExpr then loop seqExpr else

            let matchExpr = MatchExprNavigator.GetByClauseExpression(expr)
            if isNotNull matchExpr then loop matchExpr else

            let ifExpr = IfExprNavigator.GetByBranchExpression(expr)
            if isNotNull ifExpr then loop ifExpr else

            let whileExpr = WhileExprNavigator.GetByDoExpression(expr)
            if isNotNull whileExpr then loop whileExpr else

            let forExpr = ForExprNavigator.GetByDoExpression(expr)
            if isNotNull forExpr then loop forExpr else

            let tryExpr = TryLikeExprNavigator.GetByTryExpression(expr)
            if isNotNull tryExpr then loop tryExpr else

            false

        loop refExpr

    let isModuleMemberStart (context: FSharpCodeCompletionContext) =
        let reference = context.ReparsedContext.Reference
        if isNull reference then false else

        let refExpr = reference.GetTreeNode().As<IReferenceExpr>()
        if isNull refExpr then false else

        let rec loop (expr: IFSharpExpression) =
            match PrefixAppExprNavigator.GetByFunctionExpression(expr) with
            | null -> expr
            | prefixAppExpr -> loop prefixAppExpr

        let expr = loop refExpr
        let doStmt = DoStatementNavigator.GetByExpression(expr)
        let moduleDecl = ModuleLikeDeclarationNavigator.GetByMember(doStmt)
        isNotNull moduleDecl

    let suggestKeywords (context: FSharpCodeCompletionContext) =
        ()

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


type FSharpKeywordLookupItem(keyword, description: string) =
    inherit FSharpKeywordLookupItemBase(keyword, KeywordSuffix.None)

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

    let keywords =
        FSharpKeywords.KeywordsWithDescription
        // todo: implement auto-popup completion strategy that will cover operators
        |> List.filter (fun (keyword, _) -> not (PrettyNaming.IsOperatorName keyword))
        |> Array.ofList

    let scriptKeywords =
        [| "__SOURCE_DIRECTORY__"
           "__SOURCE_FILE__"
           "__LINE__" |]

    override x.IsAvailable _ = true
    override x.GetDefaultRanges(context) = context.Ranges
    override x.GetLookupFocusBehaviour _ = LookupFocusBehaviour.Soft

    override x.AddLookupItems(context, collector) =
        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        if isNotNull reference && reference.IsQualified then false else

        let tokenBeforeCaret = context.TokenBeforeCaret
        if isNull tokenBeforeCaret then false else

        let fcsCompletionContext = context.FcsCompletionContext
        match fcsCompletionContext.CompletionContext, tokenBeforeCaret.GetTokenType() with
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

        for keyword, description in keywords do
            let item = FSharpKeywordLookupItem(keyword, description)
            item.InitializeRanges(context.Ranges, context.BasicContext)
            markRelevance item CLRLookupItemRelevance.Keywords
            collector.Add(item)

        if context.BasicContext.File.Language.Is<FSharpScriptLanguage>() then
            for keyword in scriptKeywords do
                let item = FSharpKeywordLookupItem(keyword, "")
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
