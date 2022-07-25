module JetBrains.ReSharper.Plugins.FSharp.Daemon.Tooltips

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Tokenization
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util.FcsTaggedText
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.RichText
open JetBrains.Util

[<SolutionComponent>]
type FSharpIdentifierTooltipProvider(lifetime, solution, presenter, xmlDocService: FSharpXmlDocService,
        textStylesService) =
    inherit IdentifierTooltipProvider<FSharpLanguage>(lifetime, solution, presenter, textStylesService)

    let richTextEscapeToHtml (text: RichText) =
        (RichText.Empty, text.GetFormattedParts()) ||> Seq.fold (fun result part ->
            result.Append(part.Text.Replace("<", "&lt;").Replace(">", "&gt;").Replace("\n", "<br>"), part.Style))

    //todo: move all the html related code to the platform
    let asDefinition (text: RichText) = "<div class='definition'><pre style='word-wrap: break-word; white-space: pre-wrap;'>" + (richTextEscapeToHtml text) + "</pre></div>"

    let asContent (text: RichText) = "<div class='content'>" + (richTextEscapeToHtml text) + "</div>"

    let createToolTip (header: RichText) (body: RichText) =
        if body.IsEmpty then asContent header else
        (asDefinition header).Append(body)

    let [<Literal>] opName = "FSharpIdentifierTooltipProvider"

    static member GetFSharpToolTipText(checkResults: FSharpCheckFileResults, token: IFSharpIdentifierToken) =
        // todo: fix getting qualifiers
        let tokenNames = [token.Name]

        let sourceFile = token.GetSourceFile()
        let coords = sourceFile.Document.GetCoordsByOffset(token.GetTreeEndOffset().Offset)
        let lineText = sourceFile.Document.GetLineText(coords.Line)
        use cookie = CompilationContextCookie.GetOrCreate(sourceFile.GetPsiModule().GetContextFromModule())

        // todo: provide tooltip for #r strings in fsx, should pass String tag
        checkResults.GetToolTip(int coords.Line + 1, int coords.Column, lineText, tokenNames, FSharpTokenTag.Identifier)

    override x.GetRichTooltip(highlighter, _) =
        if not highlighter.IsValid then emptyPresentation else

        let psiServices = solution.GetPsiServices()
        if not psiServices.Files.AllDocumentsAreCommitted || psiServices.Caches.HasDirtyFiles then emptyPresentation else

        let document = highlighter.Document
        match document.GetPsiSourceFile(solution) with
        | null -> emptyPresentation
        | sourceFile when not (sourceFile.IsValid()) -> emptyPresentation
        | sourceFile ->

        let documentRange = DocumentRange(document, highlighter.Range)
        match x.GetPsiFile(sourceFile, documentRange).As<IFSharpFile>() with
        | null -> emptyPresentation
        | fsFile ->

        match fsFile.FindTokenAt(documentRange.StartOffset).As<IFSharpIdentifierToken>() with
        | null -> emptyPresentation
        | token ->

        match fsFile.GetParseAndCheckResults(true, opName) with
        | None -> emptyPresentation
        | Some results ->
            let (ToolTipText layouts) = FSharpIdentifierTooltipProvider.GetFSharpToolTipText(results.CheckResults, token)

            layouts |> List.collect (function
                | ToolTipElement.None -> []
                | ToolTipElement.CompositionError errorText -> [ RichText(errorText) ]

                | ToolTipElement.Group(overloads) ->
                    overloads |> List.map (fun overload ->
                        let header =
                            [ if not (isEmpty overload.MainDescription) then
                                yield overload.MainDescription |> richText

                              if not overload.TypeMapping.IsEmpty then
                                yield overload.TypeMapping |> List.map richText |> richTextJoin "\n" ]
                            |> richTextJoin "\n\n"

                        let body =
                            [ match xmlDocService.GetXmlDoc(overload.XmlDoc) with
                              | null -> ()
                              | xmlDocText -> yield xmlDocText.RichText

                              match overload.Remarks with
                              | Some remarks when not (isEmpty remarks) ->
                                yield remarks |> richText |> asContent
                              | _ -> () ]
                            |> richTextJoin "\n\n"

                        createToolTip header body))
            |> richTextJoin IdentifierTooltipProvider.RIDER_TOOLTIP_SEPARATOR
            |> RichTextBlock

    interface IFSharpIdentifierTooltipProvider
