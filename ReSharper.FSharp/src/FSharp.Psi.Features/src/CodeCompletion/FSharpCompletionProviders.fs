namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open System
open FSharp.Compiler
open FSharp.Compiler.SourceCodeServices
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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Cs.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files.SandboxFiles
open JetBrains.Util

type FSharpLookupItemsProviderBase(logger: ILogger, getAllSymbols, filterResolved) =
    let [<Literal>] opName = "FSharpLookupItemsProviderBase"
    
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
            match fsFile.GetParseAndCheckResults(true, opName) with
            | None -> false
            | Some results ->

            let skipFsiModules =
                // Workaround for FSI_0123 modules generated in sandboxes 
                fsFile.Language.Is<FSharpScriptLanguage>() &&
                fsFile.GetPsiModule() :? SandboxPsiModule

            let isFsiModuleToSkip (item: FSharpDeclarationListItem) =
                not (Array.isEmpty item.NamespaceToOpen) &&
                item.Name.StartsWith(PrettyNaming.FsiDynamicModulePrefix, StringComparison.Ordinal)

            let checkResults = results.CheckResults
            let parseResults = fsFile.ParseResults
            let line = int context.Coords.Line + 1
            let lineText = context.LineText

            let getAllSymbols () = getAllSymbols checkResults
            try
                let completionInfo =
                    checkResults
                        .GetDeclarationListInfo(parseResults, line, lineText, context.PartialLongName,
                                                getAllSymbols, filterResolved).RunAsTask()

                if completionInfo.Items.IsEmpty() then false else

                context.XmlDocService <- basicContext.Solution.GetComponent<FSharpXmlDocService>()
                context.DisplayContext <- completionInfo.DisplayContext

                if completionInfo.IsError then
                    let item = Seq.head completionInfo.Items
                    let lookupItem = FSharpErrorLookupItem(item)
                    lookupItem.InitializeRanges(context.Ranges, basicContext)
                    collector.Add(lookupItem)
                else
                    for item in completionInfo.Items do
                        if skipFsiModules && isFsiModuleToSkip item then () else

                        let lookupItem = FSharpLookupItem(item, context)
                        lookupItem.InitializeRanges(context.Ranges, basicContext)
                        collector.Add(lookupItem)
                true
            with
            | OperationCanceled -> reraise()
            | e ->
                let path = basicContext.SourceFile.GetLocation().FullPath
                let coords = context.Coords
                logger.LogMessage(LoggingLevel.WARN, "Getting completions at location: {0}: {1}", path, coords)
                logger.LogExceptionSilently(e)
                false
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
    inherit FSharpLookupItemsProviderBase(logger, assemblyContentProvider.GetLibrariesEntities, true)

    interface ICodeCompletionItemsProvider with
        member x.IsAvailable(context) =
            let settings = context.BasicContext.ContextBoundSettingsStore
            if settings.GetValue(fun (key: FSharpOptions) -> key.EnableOutOfScopeCompletion) then obj() else null

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
