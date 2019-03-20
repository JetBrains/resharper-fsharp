module JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages.Tooltips

open System
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.RichText
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

let [<Literal>] RiderTooltipSeparator = "_RIDER_HORIZONTAL_LINE_TOOLTIP_SEPARATOR_"

[<SolutionComponent>]
type FSharpIdentifierTooltipProvider(lifetime, solution, presenter, xmlDocService: FSharpXmlDocService) =
    inherit IdentifierTooltipProvider<FSharpLanguage>(lifetime, solution, presenter)

    let [<Literal>] opName = "FSharpIdentifierTooltipProvider"

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
        match x.GetPsiFile(sourceFile, documentRange).As<IFSharpFile>() with
        | null -> String.Empty
        | fsFile ->

        match fsFile.FindTokenAt(documentRange.StartOffset).As<FSharpIdentifierToken>() with
        | null -> String.Empty
        | token ->

        match fsFile.GetParseAndCheckResults(true, opName) with
        | None -> String.Empty
        | Some results ->

        let checkResults = results.CheckResults
        let coords = document.GetCoordsByOffset(token.GetTreeEndOffset().Offset)
        let names = token.GetQualifiersAndName() |> List.ofArray
        let lineText = sourceFile.Document.GetLineText(coords.Line)

        // todo: provide tooltip for #r strings in fsx, should pass String tag
        let getTooltip = checkResults.GetToolTipText(int coords.Line + 1, int coords.Column, lineText, names, FSharpTokenTag.Identifier)
        let result = ResizeArray()
        match getTooltip.RunAsTask() with
        | FSharpToolTipText(tooltips) ->
            tooltips |> List.iter (function
                | FSharpToolTipElement.None
                | FSharpToolTipElement.CompositionError _ -> ()

                | FSharpToolTipElement.Group(overloads) ->
                    overloads |> List.iter (fun overload ->
                        let text = overload.MainDescription.TrimStart()
                        match xmlDocService.GetXmlDoc(overload.XmlDoc) with
                        | null when not (text.IsNullOrWhitespace()) -> result.Add(text)
                        | xmlDocText ->

                        let xmlDocText = xmlDocText.Text
                        match text.IsNullOrWhitespace(), xmlDocText.IsNullOrWhitespace() with
                        | false, false -> result.Add(text + "\n\n" + xmlDocText)
                        | false, _ -> result.Add(text)
                        | _, false -> result.Add(xmlDocText)
                        | _ -> ()))

        result.Join(RiderTooltipSeparator)

    override x.GetRichTooltip(highlighter) =
        RichTextBlock(x.GetTooltip(highlighter))

    interface IFSharpIdentifierTooltipProvider
