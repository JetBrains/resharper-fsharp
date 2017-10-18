namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpLookupItemsProvider(logger: ILogger) =
    inherit FSharpItemsProviderBase()

    let getEntitiesStub = fun () -> []

    override x.AddLookupItems(context, collector) =
        if not context.ShouldComplete then false else

        let basicContext = context.BasicContext
        match basicContext.File with
        | :? IFSharpFile as fsFile when fsFile.ParseResults.IsSome ->
            match fsFile.GetParseAndCheckResults(true) with
            | Some results ->
                let parseResults = fsFile.ParseResults
                let line, column = int context.Coords.Line + 1, int context.Coords.Column
                let lineText = context.LineText
                let qualifiers, partialName = context.Names
                let getIconId = getIconId >> Option.ofObj
                try
                    let completions =
                        results.CheckResults
                            .GetDeclarationListInfo(parseResults, line, column, lineText, qualifiers, partialName,
                                                    getEntitiesStub, getIconId).RunAsTask().Items

                    if Array.isEmpty completions then false else

                    let xmlDocService = basicContext.Solution.GetComponent<FSharpXmlDocService>()
                    for item in completions |> Seq.filter (fun c -> c.NamespaceToOpen.IsNone) do
                        let lookupItem = FSharpLookupItem(item, xmlDocService)
                        lookupItem.InitializeRanges(context.Ranges, basicContext)
                        collector.Add(lookupItem)
                        collector.CheckForInterrupt()
                    true
                with e ->
                    let path = basicContext.SourceFile.GetLocation().FullPath
                    let coords = context.Coords
                    logger.LogMessage(LoggingLevel.WARN, "Getting completions at location: {0}: {1}", path, coords)
                    logger.LogExceptionSilently(e)
                    false
            | _ -> false
        | _ -> false
