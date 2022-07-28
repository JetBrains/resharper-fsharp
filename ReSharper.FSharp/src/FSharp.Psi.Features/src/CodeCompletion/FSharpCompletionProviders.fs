namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open System
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Syntax
open JetBrains.Application.Settings
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files.SandboxFiles
open JetBrains.Util

type FSharpLookupItemsProviderBase(logger: ILogger, filterResolved, getAllSymbols) =
    let [<Literal>] opName = "FSharpLookupItemsProviderBase"

    let isAttributeReference (context: FSharpCodeCompletionContext) =
        let reference = context.ReparsedContext.Reference
        if isNull reference then false else

        match reference.GetTreeNode() with
        | :? ITypeReferenceName as name -> isNotNull (AttributeNavigator.GetByReferenceName(name))
        | _ -> false

    member x.GetDefaultRanges(context: ISpecificCodeCompletionContext) =
        context |> function | :? FSharpCodeCompletionContext as context -> context.Ranges | _ -> null

    member x.IsAvailable(context: ISpecificCodeCompletionContext) =
        let fsContext = context.As<FSharpCodeCompletionContext>()
        if isNull fsContext then null else

        let tokenType = getTokenType fsContext.TokenAtCaret
        let tokenBeforeType = getTokenType fsContext.TokenBeforeCaret

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
        let fcsContext = context.FcsCompletionContext

        match fcsContext.CompletionContext with
        | Some(CompletionContext.Invalid) -> false
        | _ ->

        let basicContext = context.BasicContext
        match context.BasicContext.File, context.GetCheckResults(opName) with
        | :? IFSharpFile as fsFile, Some checkResults ->
            let settings = basicContext.ContextBoundSettingsStore
            let addImportItems = settings.GetValue(fun (key: FSharpOptions) -> key.EnableOutOfScopeCompletion)

            let skipFsiModules =
                // Workaround for FSI_0123 modules generated in sandboxes
                fsFile.Language.Is<FSharpScriptLanguage>() &&
                fsFile.GetPsiModule() :? SandboxPsiModule

            let isFsiModuleToSkip (item: RiderDeclarationListItems) =
                not (Array.isEmpty item.NamespaceToOpen) &&
                item.Name.StartsWith(PrettyNaming.FsiDynamicModulePrefix, StringComparison.Ordinal)

            let parseResults = fsFile.ParseResults
            let line = int fcsContext.Coords.Line + 1

            let isAttributeReferenceContext = isAttributeReference context
            let getAllSymbols () = getAllSymbols (checkResults, context.BasicContext.Solution)

            try
                let itemLists =
                    checkResults.GetDeclarationListSymbols(parseResults, line, fcsContext.LineText,
                        fcsContext.PartialName, isAttributeReferenceContext, getAllSymbols)

                for list in itemLists do
                    if list.Name = ".. .." then () else
                    let resolved = list.NamespaceToOpen.IsEmpty()
                    if (not addImportItems) && not resolved || filterResolved && resolved then () else
                    if skipFsiModules && isFsiModuleToSkip list then () else

                    let lookupItem = FcsLookupItem(list, context)
                    lookupItem.InitializeRanges(context.Ranges, basicContext)
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


[<Language(typeof<FSharpLanguage>)>]
type FSharpLookupItemsProvider(logger: ILogger) =
    inherit FSharpLookupItemsProviderBase(logger, false, fun (checkResults, _) ->
        let assemblySignature = checkResults.PartialAssemblySignature
        let getSymbolsAsync = async {
            return AssemblyContent.GetAssemblySignatureContent AssemblyContentType.Full assemblySignature }
        getSymbolsAsync.RunAsTask())

    interface ICodeCompletionItemsProvider with
        member x.IsAvailable(context) = base.IsAvailable(context)
        member x.GetDefaultRanges(context) = base.GetDefaultRanges(context)
        member x.AddLookupItems(context, collector, _) =
            base.AddLookupItems(context :?> FSharpCodeCompletionContext, collector)

        member x.TransformItems(_, _, _) = ()
        member x.DecorateItems(_, _, _) = ()

        member x.GetLookupFocusBehaviour(_, _) = LookupFocusBehaviour.Soft
        member x.GetAutocompletionBehaviour(_, _) = AutocompletionBehaviour.NoRecommendation

        member x.IsDynamic = false
        member x.IsFinal = false
        member x.SupportedCompletionMode = CompletionMode.Single
        member x.SupportedEvaluationMode = EvaluationMode.Light


[<Language(typeof<FSharpLanguage>)>]
type FSharpRangesProvider() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override x.GetDefaultRanges(context) = context.Ranges
    override x.SupportedCompletionMode = CompletionMode.All
    override x.SupportedEvaluationMode = EvaluationMode.Full


[<Language(typeof<FSharpLanguage>)>]
type FSharpLibraryScopeLookupItemsProvider(logger: ILogger, assemblyContentProvider: FSharpAssemblyContentProvider) =
    inherit FSharpLookupItemsProviderBase(logger, true, assemblyContentProvider.GetLibrariesEntities)

    interface ICodeCompletionItemsProvider with
        member x.IsAvailable(context) =
            let settings = context.BasicContext.ContextBoundSettingsStore
            let enabled = settings.GetValue(fun (key: FSharpOptions) -> key.EnableOutOfScopeCompletion)

            if enabled then base.IsAvailable(context) else null

        member x.GetDefaultRanges(context) = base.GetDefaultRanges(context)
        member x.AddLookupItems(context, collector, _) =
            base.AddLookupItems(context :?> FSharpCodeCompletionContext, collector)

        member x.TransformItems(_, _, _) = ()
        member x.DecorateItems(_, _, _) = ()

        member x.GetLookupFocusBehaviour(_, _) = LookupFocusBehaviour.Soft
        member x.GetAutocompletionBehaviour(_, _) = AutocompletionBehaviour.NoRecommendation

        member x.IsDynamic = false
        member x.IsFinal = false
        member x.SupportedCompletionMode = CompletionMode.Single
        member x.SupportedEvaluationMode = EvaluationMode.Full


[<SolutionComponent>]
type FSharpAutocompletionStrategy() =
    interface IAutomaticCodeCompletionStrategy with
        member x.Language = FSharpLanguage.Instance :> _
        member x.AcceptsFile(file, _) = file :? IFSharpFile

        member x.AcceptTyping(char, _, _) = char.IsLetterFast() || char = '.'
        member x.ProcessSubsequentTyping(char, _) = char.IsIdentifierPart()

        member x.IsEnabledInSettings(_, _) = AutopopupType.SoftAutopopup
        member x.ForceHideCompletion = false
