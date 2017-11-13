module JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages.Tooltips

open System
open System.Collections.Generic
open System.Threading
open System.Web
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.UI.RichText
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Psi.Caches

let [<Literal>] RiderTooltipSeparator = "_RIDER_HORIZONTAL_LINE_TOOLTIP_SEPARATOR_"

[<SolutionComponent>]
type FSharpIdentifierTooltipProvider(lifetime, solution, presenter, logger, xmlDocService: FSharpXmlDocService) =
    inherit IdentifierTooltipProvider<FSharpLanguage>(lifetime, solution, presenter)

    let isNullOrWhiteSpace = RichTextBlock.IsNullOrWhiteSpace

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
                match fsFile.GetParseAndCheckResults(true) with
                | Some results ->
                    let checkResults = results.CheckResults
                    let coords = document.GetCoordsByOffset(token.GetTreeEndOffset().Offset)
                    let names = token.GetQualifiersAndName() |> List.ofArray
                    let lineText = sourceFile.Document.GetLineText(coords.Line)

                    // todo: provide tooltip for #r strings in fsx, should pass String tag
                    let getTooltip = checkResults.GetToolTipText(int coords.Line + 1, int coords.Column, lineText, names, FSharpTokenTag.Identifier)
                    let result = List()
                    match getTooltip.RunAsTask() with
                    | FSharpToolTipText(tooltips) ->
                        tooltips |> List.iter (function
                            | FSharpToolTipElement.Group(overloads) ->
                                overloads |> List.iter (fun overload ->
                                    let text = overload.MainDescription
                                    match xmlDocService.GetXmlDoc(overload.XmlDoc) with
                                    | null when not (text.IsNullOrWhitespace()) -> result.Add(text)
                                    | xmlDocText ->
                                        let xmlDocText = xmlDocText.Text
                                        match text.IsNullOrWhitespace(), xmlDocText.IsNullOrWhitespace() with
                                        | false, false -> result.Add(text + "\n\n" + xmlDocText)
                                        | false, _ -> result.Add(text)
                                        | _, false -> result.Add(xmlDocText)
                                        | _ ->  ())
                            | _ -> ())
                    result.Join(RiderTooltipSeparator)
                | _ -> String.Empty
            | _ -> String.Empty
        | _ -> String.Empty

    override x.GetRichTooltip(highlighter) =
        RichTextBlock(x.GetTooltip(highlighter))

    interface IFSharpIdentifierTooltipProvider