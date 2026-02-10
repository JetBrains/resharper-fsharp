namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open System
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Syntax
open JetBrains.Application
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files.SandboxFiles
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FcsLookupItemsProvider(logger: ILogger) =
    member x.GetDefaultRanges(context: ISpecificCodeCompletionContext) =
        context |> function | :? FSharpCodeCompletionContext as context -> context.Ranges | _ -> null

    member x.IsAvailable(context: ISpecificCodeCompletionContext) =
        let fsContext = context.As<FSharpCodeCompletionContext>()
        if isNull fsContext then null else

        let isNamedUnionCaseFieldsPat =
            let reference = fsContext.ReparsedContext.Reference
            let parametersOwnerPat = FSharpCompletionUtil.getParametersOwnerPatFromReference reference
            isNotNull parametersOwnerPat && parametersOwnerPat.Parameters.SingleItem :? INamedUnionCaseFieldsPat

        if isNamedUnionCaseFieldsPat then null else

        let tokenType = getTokenType fsContext.TokenAtCaret
        let tokenBeforeType = getTokenType fsContext.TokenBeforeCaret

        // 1.
        // 1.m
        // 1.ma
        if tokenBeforeType == FSharpTokenType.DECIMAL ||
           tokenBeforeType == FSharpTokenType.IEEE64 ||
           tokenBeforeType == FSharpTokenType.RESERVED_LITERAL_FORMATS then null else

        // :{caret}:
        if fsContext.InsideToken && tokenType == FSharpTokenType.COLON_COLON then null else

        // foo // comment{caret}
        if tokenBeforeType == FSharpTokenType.LINE_COMMENT then null else

        // "{caret}"
        // " foo {caret}EOF
        // todo: check unfinished strings/comments instead of `isNull tokenType`
        if isNotNull tokenBeforeType && (fsContext.InsideToken || isNull tokenType) &&
                (tokenBeforeType.IsComment ||
                 FSharpTokenType.Strings[tokenBeforeType] ||
                 tokenBeforeType.IsConstantLiteral) then null else

        obj()

    member x.AddLookupItems(context: FSharpCodeCompletionContext, collector: IItemsCollector) =
        let reparsedContext = context.ReparsedContext
        let reparsedFcsContext = reparsedContext.GetFcsContext()

        match reparsedFcsContext.CompletionContext with
        | Some(CompletionContext.Invalid) -> false
        | _ ->

        let basicContext = context.BasicContext
        match context.BasicContext.File, context.GetCheckResults() with
        | :? IFSharpFile as fsFile, Some checkResults ->
            let skipFsiModules =
                // Workaround for FSI_0123 modules generated in sandboxes
                fsFile.GetSourceFile().LanguageType.Is<FSharpScriptProjectFileType>() &&
                fsFile.GetPsiModule() :? SandboxPsiModule

            let isFsiModuleToSkip (item: RiderDeclarationListItems) =
                not (Array.isEmpty item.NamespaceToOpen) &&
                item.Name.StartsWith(PrettyNaming.FsiDynamicModulePrefix, StringComparison.Ordinal)

            let parseResults = fsFile.ParseResults
            let fcsContext = context.FcsCompletionContext
            let line = int fcsContext.Coords.Line + 1

            try
                let itemLists =
                    checkResults.GetDeclarationListSymbols(parseResults, line, fcsContext.LineText,
                        fcsContext.PartialName, context.IsInAttributeContext)

                for list in itemLists do
                    Interruption.Current.CheckAndThrow()

                    if list.Name = ".. .." then () else
                    if skipFsiModules && isFsiModuleToSkip list then () else

                    let lookupItem = FcsLookupItem(list, context, Ranges = context.Ranges)
                    collector.Add(lookupItem)
                false
            with
            | OperationCanceled -> reraise()
            | e ->
                let path = basicContext.SourceFile.GetLocation().FullPath
                let coords = fcsContext.Coords
                logger.LogMessage(LoggingLevel.WARN, "Getting completions at location: {0}: {1}", path, coords)
                logger.LogExceptionSilently(e)
                false
        | _ -> false

    interface ICodeCompletionItemsProvider with
        member x.IsAvailable(context) = x.IsAvailable(context)
        member x.GetDefaultRanges(context) = x.GetDefaultRanges(context)
        member x.AddLookupItems(context, collector, _) =
            x.AddLookupItems(context :?> FSharpCodeCompletionContext, collector)

        member x.TransformItems(_, _, _) = ()
        member x.DecorateItems(_, _, _) = ()

        member x.GetLookupFocusBehaviour(_, _) = LookupFocusBehaviour.Soft
        member x.GetAutoAcceptBehaviour(_, _) = AutoAcceptBehaviour.NoRecommendation

        member x.IsDynamic = false
        member x.IsFinal = false
        member x.SupportedCompletionMode = CompletionMode.Single
        member x.SupportedEvaluationMode = EvaluationMode.Light
