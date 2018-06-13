namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open System
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Cs.CodeCompletion
open JetBrains.ReSharper.Psi
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpLookupItemsProviderBase(logger: ILogger, getAllSymbols, filterResolved) =
    member x.GetDefaultRanges(context: ISpecificCodeCompletionContext) =
        context |> function | :? FSharpCodeCompletionContext as context -> context.Ranges | _ -> null

    member x.IsAvailable(context: ISpecificCodeCompletionContext) =
        context |> function | :? FSharpCodeCompletionContext -> obj() | _ -> null

    member x.AddLookupItems(context: FSharpCodeCompletionContext, collector: IItemsCollector) =
        match context.FsCompletionContext with
        | Some (CompletionContext.Invalid) -> false
        | _ ->

        let basicContext = context.BasicContext
        match basicContext.File with
        | :? IFSharpFile as fsFile when fsFile.ParseResults.IsSome ->
            match fsFile.GetParseAndCheckResults(true) with
            | Some results ->
                let checkResults = results.CheckResults
                let parseResults = fsFile.ParseResults
                let line, column = int context.Coords.Line + 1, int context.Coords.Column
                let lineText = context.LineText
                let getIconId (symbol, context) =
                        // todo: provide symbol and display context in FCS items, calc this only when needed
                        let icon = getIconId symbol
                        let retType =
                            match getReturnType symbol with
                            | Some t -> t.Format(context)
                            | _ -> null
                        Some { Icon = icon; ReturnType = retType }

                let getAllSymbols () = getAllSymbols checkResults 
                try
                    let completionInfo =
                        checkResults
                            .GetDeclarationListInfo(parseResults, line, lineText, context.PartialLongName,
                                                    getAllSymbols, filterResolved).RunAsTask()

                    let completionItems = completionInfo.Items
                    if completionItems.IsEmpty() then false else

                    let xmlDocService = basicContext.Solution.GetComponent<FSharpXmlDocService>()
                    for item in completionItems do
                        let (lookupItem: TextLookupItemBase) =
                            if item.Glyph = FSharpGlyph.Error
                            then FSharpErrorLookupItem(item) :> _
                            else FSharpLookupItem(item, context, completionInfo.DisplayContext, xmlDocService) :> _

                        lookupItem.InitializeRanges(context.Ranges, basicContext)
                        collector.Add(lookupItem)
                    true
                with
                | :? OperationCanceledException -> reraise()
                | e ->
                    let path = basicContext.SourceFile.GetLocation().FullPath
                    let coords = context.Coords
                    logger.LogMessage(LoggingLevel.WARN, "Getting completions at location: {0}: {1}", path, coords)
                    logger.LogExceptionSilently(e)
                    false
            | _ -> false
        | _ -> false


[<Language(typeof<FSharpLanguage>)>]
type FSharpLookupItemsProvider(logger: ILogger) =
    inherit FSharpLookupItemsProviderBase(logger, (fun checkResults ->
        let assemblySignature = checkResults.PartialAssemblySignature
        let getSymbolsAsync = async {
            return AssemblyContentProvider.getAssemblySignatureContent AssemblyContentType.Full assemblySignature }
        getSymbolsAsync.RunAsTask()), false)

    interface ICodeCompletionItemsProvider with
        member x.IsAvailable(context) = base.IsAvailable(context)
        member x.GetDefaultRanges(context) = base.GetDefaultRanges(context)
        member x.AddLookupItems(context, collector, _) =
            base.AddLookupItems(context :?> FSharpCodeCompletionContext, collector)

        member x.TransformItems(context, collector, data) = ()
        member x.DecorateItems(context, collector, data) = ()

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
    inherit FSharpLookupItemsProviderBase(logger, assemblyContentProvider.GetLibrariesEntities, true)

    interface ISlowCodeCompletionItemsProvider with
        member x.IsAvailable(context) = base.IsAvailable(context)
        member x.AddLookupItems(context, collector, data) =
            base.AddLookupItems(context :?> FSharpCodeCompletionContext, collector)

        member x.SupportedEvaluationMode = EvaluationMode.Full


[<SolutionComponent>]
type FSharpAutocompletionStrategy() =
    interface IAutomaticCodeCompletionStrategy with
        member x.Language = FSharpLanguage.Instance :> _
        member x.AcceptsFile(file, textControl) = file :? IFSharpFile

        member x.AcceptTyping(char, _, _) = char.IsLetterFast() || char = '.'
        member x.ProcessSubsequentTyping(char, _) = char.IsIdentifierPart()

        member x.IsEnabledInSettings(_, _) = AutopopupType.SoftAutopopup
        member x.ForceHideCompletion = false
