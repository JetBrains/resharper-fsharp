namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
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

    member val IsReparseContextAware = false with get, set
    
    interface IDescriptionProvidingLookupItem with
        member x.GetDescription() = RichTextBlock(description)


type FSharpHashDirectiveLookupItem(directive, suffix) =
    inherit FSharpKeywordLookupItemBase(directive, suffix)


module FSharpKeywordsProvider =
    let reparseContextAwareKeywords =
        [| "abstract"
           "and!"
           "const"
           "default"
           "do!"
           "exception"
           "extern"
           "inherit"
           "inline" // never suggested due to invalid Fcs context
           "let!"
           "match!"
           "member"
           "module"
           "mutable" // never suggested due to invalid Fcs context
           "namespace"
           "of"
           "open"
           "override"
           "return!"
           "type"
           "static"
           "use!"
           "val"
           "void"
           "yield!" |]
        |> HashSet

    let keywordsWithDescription = FSharpKeywords.KeywordsWithDescription

    let alwaysSuggestedKeywords =
        keywordsWithDescription
        |> List.filter (fun (keyword, _) ->
            not (reparseContextAwareKeywords.Contains(keyword)) &&
            not (PrettyNaming.IsOperatorDisplayName keyword))
        |> List.map fst

    let keywordItems =
        let keywordItems = Dictionary()
            
        for keyword, description in keywordsWithDescription do
            keywordItems.Add(keyword, FSharpKeywordLookupItem(keyword, description))

        for keyword in reparseContextAwareKeywords do
            if not (keywordItems.ContainsKey(keyword)) then
                keywordItems.Add(keyword, FSharpKeywordLookupItem(keyword, ""))

        keywordItems

    let getReferenceOwner (context: FSharpCodeCompletionContext) =
        let reference = context.ReparsedContext.Reference
        if isNull reference then null else

        reference.GetTreeNode()

    let getOuterPrefixAppFromFunctionExpr expr =
        let rec loop (expr: IFSharpExpression) =
            match PrefixAppExprNavigator.GetByFunctionExpression(expr) with
            | null -> expr
            | prefixAppExpr -> loop prefixAppExpr
        loop expr

    let isInComputationExpression (context: FSharpCodeCompletionContext) =
        let reference = context.ReparsedContext.Reference
        if isNull reference then false, false else

        let refExpr = reference.GetTreeNode().As<IReferenceExpr>()
        if isNull refExpr then false, false else

        let computationExpr, isInLet = tryGetEffectiveParentComputationExpression refExpr
        isNotNull computationExpr, isInLet

    let isModuleMemberStart (context: FSharpCodeCompletionContext) =
        match getReferenceOwner context with
        | :? ITypeReferenceName as referenceName ->
            let moduleAbbreviationDecl = ModuleAbbreviationDeclarationNavigator.GetByTypeName(referenceName)
            let moduleDecl = ModuleLikeDeclarationNavigator.GetByMember(moduleAbbreviationDecl)
            isNotNull moduleAbbreviationDecl, moduleAbbreviationDecl :> IModuleMember, moduleDecl

        | :? IReferenceExpr as refExpr ->
            let expr = getOuterPrefixAppFromFunctionExpr refExpr
            let doStmt = ExpressionStatementNavigator.GetByExpression(expr)
            let moduleDecl = ModuleLikeDeclarationNavigator.GetByMember(doStmt)
            isNotNull moduleDecl, doStmt :> _, moduleDecl

        | _ -> false, null, null

    let isAtTypeInOpen (context: FSharpCodeCompletionContext) =
        let referenceOwner = getReferenceOwner context
        let referenceName = referenceOwner.As<ITypeReferenceName>()
        if isNull referenceName || isNotNull referenceName.Qualifier then false else

        let rec loop (referenceName: ITypeReferenceName) =
            let qualifiedReferenceName = TypeReferenceNameNavigator.GetByQualifier(referenceName)
            if isNotNull qualifiedReferenceName then loop qualifiedReferenceName else
            isNotNull (OpenStatementNavigator.GetByReferenceName(referenceName))

        loop referenceName

    let allowsNamespace (moduleMember: IModuleMember) (moduleDecl: IModuleLikeDeclaration) =
        match moduleDecl with
        | :? INamespaceDeclaration | :? IAnonModuleDeclaration ->
            not (moduleMember :? IModuleAbbreviationDeclaration)
        | _ -> false

    let mayStartTypeMember (context: FSharpCodeCompletionContext) =
        // todo: get element from the context
        match getReferenceOwner context with
        | :? IReferenceExpr ->
            false

        | :? ITypeReferenceName as referenceName ->
            let typeUsage = NamedTypeUsageNavigator.GetByReferenceName(referenceName)
            let declaration = TypeUsageOrUnionCaseDeclarationNavigator.GetByTypeUsage(typeUsage)
            isNotNull declaration

        | _ -> true

    let mayStartInheritExpr (context: FSharpCodeCompletionContext) =
        match getReferenceOwner context with
        | :? IReferenceExpr as refExpr ->
            let appExpr = getOuterPrefixAppFromFunctionExpr refExpr
            isNotNull (ComputationExprNavigator.GetByExpression(appExpr))
        | _ -> false

    let isAtConstTypePosition (context: FSharpCodeCompletionContext) =
        let treeNode = context.ReparsedContext.TreeNode
        isNotNull treeNode &&

        let prevToken = treeNode.GetPreviousMeaningfulToken()
        let prevTokenType = getTokenType prevToken
        prevTokenType == FSharpTokenType.LESS || prevTokenType == FSharpTokenType.COMMA

    let mayBeUnionCaseDecl (context: FSharpCodeCompletionContext) =
        match getReferenceOwner context with
        | :? ITypeReferenceName as referenceName ->
            let typeUsage = NamedTypeUsageNavigator.GetByReferenceName(referenceName)
            isNotNull (TypeUsageOrUnionCaseDeclarationNavigator.GetByTypeUsage(typeUsage)) &&

            match referenceName.TypeArgumentList with
            | :? IPostfixAppTypeArgumentList as list ->
                let typeArgs = list.TypeUsages
                typeArgs.Count = 1 &&

                match typeArgs[0] with
                | :? INamedTypeUsage as argTypeUsage ->
                    let argReferenceName = argTypeUsage.ReferenceName
                    not argReferenceName.IsQualified && isNull argReferenceName.TypeArgumentList
                | _ -> false
            | _ -> false
        | _ -> false

    let mayBeInTypeUsage (context: FSharpCodeCompletionContext) =
        match getReferenceOwner context with
        | :? IReferenceExpr -> false
        | :? ITypeReferenceName as referenceName ->
            isNotNull (NamedTypeUsageNavigator.GetByReferenceName(referenceName))
        | _ -> true

    let inReferenceExpression (context: FSharpCodeCompletionContext) =
        getReferenceOwner context :? IFSharpQualifiableReferenceOwner

    let suggestKeywords (context: FSharpCodeCompletionContext) = seq {
        let isSignatureFile = context.NodeInFile.IsFSharpSigFile()

        let isModuleMemberStart, moduleMember, moduleDecl = isModuleMemberStart context
        if isModuleMemberStart || isSignatureFile then
            "exception"
            "extern"
            "module"
            "type" // todo: visibility before type recovery

            let exprStmt = moduleMember.As<IExpressionStatement>()
            if isNull exprStmt || Seq.isEmpty exprStmt.AttributeListsEnumerable then
                "open"

                if allowsNamespace moduleMember moduleDecl then
                    "namespace"

        if isAtTypeInOpen context then
            "type"

        if isSignatureFile then
            "val"

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

        if mayStartTypeMember context && not (mayBeUnionCaseDecl context) then
            "abstract"
            "default"
            "inherit"
            "member"
            "override"
            "static"
            "val"

        if mayBeInTypeUsage context then
            "void"

        if not (inReferenceExpression context) || mayBeUnionCaseDecl context then
            "of"

        if mayStartInheritExpr context then
            "inherit"
        
        if isAtConstTypePosition context then
            "const"
    }


[<Language(typeof<FSharpLanguage>)>]
type FSharpKeywordsRule() =
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

        let prevMeaningfulToken = skipMatchingTokensBefore isInlineSpaceOrComment tokenBeforeCaret
        if getTokenType prevMeaningfulToken == FSharpTokenType.AS then false else

        match fcsCompletionContext.CompletionContext, getTokenType tokenBeforeCaret with
        | Some(CompletionContext.Invalid), tokenBeforeType when tokenBeforeType != FSharpTokenType.HASH -> false
        | _, tokenBeforeType ->

        if tokenBeforeType == FSharpTokenType.LINE_COMMENT ||
           tokenBeforeType == FSharpTokenType.DEAD_CODE ||
           tokenBeforeType == FSharpTokenType.DOT ||
           tokenBeforeType == FSharpTokenType.RESERVED_LITERAL_FORMATS ||
           isNotNull tokenBeforeType && tokenBeforeType.IsConstantLiteral ||
           tokenBeforeCaret == context.TokenAtCaret && isNotNull tokenBeforeType &&
               (tokenBeforeType.IsComment || FSharpTokenType.Strings[tokenBeforeType] || tokenBeforeType.IsConstantLiteral)
        then false else

        if not fcsCompletionContext.PartialName.QualifyingIdents.IsEmpty then false else

        let add contextAware (keywords: string seq) =
            for keyword in keywords do
                match tryGetValue keyword FSharpKeywordsProvider.keywordItems with
                | None -> ()
                | Some item ->

                item.InitializeRanges(context.Ranges, context.BasicContext)
                item.IsReparseContextAware <- contextAware
                markRelevance item CLRLookupItemRelevance.Keywords

                match keyword with
                | "true" | "false" | "null" ->
                    // use the same relevance as module members
                    // todo: add F#-specific relevance
                    markRelevance item CLRLookupItemRelevance.Methods
                | _ -> ()

                collector.Add(item)

        if isNotNull reference && isNotNull (OpenStatementNavigator.GetByReferenceName(reference.GetElement().As())) then
            add true ["type"; "global"]
            true else

        add false FSharpKeywordsProvider.alwaysSuggestedKeywords
        add true (FSharpKeywordsProvider.suggestKeywords context)

        if context.BasicContext.File.GetSourceFile().LanguageType.Is<FSharpScriptProjectFileType>() then
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
