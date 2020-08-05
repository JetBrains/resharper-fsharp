module JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages.Tooltips

open System
open System.Collections.Generic
open FSharp.Compiler.Layout
open FSharp.Compiler.SourceCodeServices
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.RichText
open JetBrains.Util

let [<Literal>] RiderTooltipSeparator = "_RIDER_HORIZONTAL_LINE_TOOLTIP_SEPARATOR_"

[<SolutionComponent>]
type FSharpIdentifierTooltipProvider(lifetime, solution, presenter, xmlDocService: FSharpXmlDocService) =
    inherit IdentifierTooltipProvider<FSharpLanguage>(lifetime, solution, presenter)

    let [<Literal>] opName = "FSharpIdentifierTooltipProvider"

    static member GetFSharpToolTipText(userOpName: string, checkResults: FSharpCheckFileResults, token: FSharpIdentifierToken) =
        // todo: fix getting qualifiers
        let tokenNames = [token.Name]

        let sourceFile = token.GetSourceFile()
        let coords = sourceFile.Document.GetCoordsByOffset(token.GetTreeEndOffset().Offset)
        let lineText = sourceFile.Document.GetLineText(coords.Line)
        use cookie = CompilationContextCookie.GetOrCreate(sourceFile.GetPsiModule().GetContextFromModule())

        use op = token.CheckerService.FcsReactorMonitor.MonitorOperation userOpName

        // todo: provide tooltip for #r strings in fsx, should pass String tag
        let getTooltip = checkResults.GetStructuredToolTipText(int coords.Line + 1, int coords.Column, lineText, tokenNames, FSharpTokenTag.Identifier, op.OperationName)
        getTooltip.RunAsTask()

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

        let result = List()
        let (FSharpToolTipText layouts) = FSharpIdentifierTooltipProvider.GetFSharpToolTipText(opName, results.CheckResults, token)
        
        layouts |> List.iter (function
            | FSharpStructuredToolTipElement.None
            | FSharpStructuredToolTipElement.CompositionError _ -> ()

            | FSharpStructuredToolTipElement.Group(overloads) ->
                overloads |> List.iter (fun overload ->
                    [ if not (isEmptyL overload.MainDescription) then
                          yield showL overload.MainDescription

                      if not overload.TypeMapping.IsEmpty then
                          yield overload.TypeMapping |> List.map showL |> String.concat "\n"

                      match xmlDocService.GetXmlDoc(overload.XmlDoc) with
                      | null -> ()
                      | xmlDocText when xmlDocText.Text.IsNullOrWhitespace() -> ()
                      | xmlDocText -> yield xmlDocText.Text

                      match overload.Remarks with
                      | Some remarks when not (isEmptyL remarks) ->
                          yield showL remarks
                      | _ -> () ]
                    |> String.concat "\n\n"
                    |> result.Add))

        result.Join(RiderTooltipSeparator)

    override x.GetRichTooltip(highlighter) =
        RichTextBlock(x.GetTooltip(highlighter))

    interface IFSharpIdentifierTooltipProvider
