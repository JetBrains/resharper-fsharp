module JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages.Tooltips

open System
open System.Collections.Generic
open System.Web
open FsAutoComplete.TipFormatter
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.RichText
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

let [<Literal>] RiderTooltipSeparator = "_RIDER_HORIZONTAL_LINE_TOOLTIP_SEPARATOR_"

[<SolutionComponent>]
type FSharpIdentifierTooltipProvider(lifetime, solution, presenter, logger) =
    inherit IdentifierTooltipProvider<FSharpLanguage>(lifetime, solution, presenter)

    override x.GetTooltip(highlighter) =
        if not highlighter.IsValid then String.Empty else

        let psiServices = solution.GetPsiServices()
        if not psiServices.Files.AllDocumentsAreCommitted || psiServices.Caches.HasDirtyFiles then String.Empty else

        let document = highlighter.Document
        match document.GetPsiSourceFile(solution) with
        | null -> String.Empty
        | sourceFile when not (sourceFile.IsValid()) -> String.Empty
        | sourceFile ->

        let documentRange = DocumentRange(document, highlighter.Range)
        match x.GetPsiFile(sourceFile, documentRange) with
        | :? IFSharpFile as fsFile ->
            match fsFile.FindTokenAt(documentRange.StartOffset) with
            | :? FSharpIdentifierToken as token ->
                match fsFile.GetParseAndCheckResults() with
                | Some results ->
                    let checkResults = results.CheckResults
                    let coords = document.GetCoordsByOffset(token.GetTreeEndOffset().Offset)
                    let names = token.GetQualifiersAndName() |> List.ofArray
                    let lineText = sourceFile.Document.GetLineText(coords.Line)

                    // todo: provide tooltip for #r strings in fsx, should pass String tag
                    let getTooltip =
                        checkResults.GetToolTipText(int coords.Line + 1, int coords.Column, lineText, names, FSharpTokenTag.Identifier)

                    // todo: don't cancel computation and use previous results instead 
                    let result = List()
                    match getTooltip.RunSynchronouslySafe(logger, "Getting F# tooltip", 2000) with
                    | FSharpToolTipText(tooltips) ->
                        tooltips |> List.iter (function
                            | FSharpToolTipElement.Group(overloads) ->
                                overloads |> List.iter (fun overload ->
                                    let text = overload.MainDescription
                                    let xmlDoc = overload.XmlDoc
                                    match HttpUtility.HtmlDecode(buildFormatComment xmlDoc) with
                                    | null -> result.Add(text)
                                    | xmlDocText -> result.Add(text + "\n\n" + xmlDocText.TrimEnd('\r', '\n')))
                            | _ -> ())
                    result.Join(RiderTooltipSeparator)
                | _ -> String.Empty
            | _ -> String.Empty
        | _ -> String.Empty

    override x.GetRichTooltip(highlighter) =
        RichTextBlock(x.GetTooltip(highlighter))

    interface IFSharpIdentifierTooltipProvider