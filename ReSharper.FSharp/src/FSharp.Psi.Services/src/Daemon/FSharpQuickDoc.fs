module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Daemon.FSharpQuickDoc

open FSharp.Compiler.EditorServices
open FSharp.Compiler.Tokenization
open JetBrains.ReSharper.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util.FcsTaggedText
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.RichText

let getFSharpToolTipText (token: IFSharpIdentifier) : ToolTipText option =
    match token.FSharpFile.GetParseAndCheckResults(true, "FSharpQuickDoc") with
    | None -> None
    | Some results ->

    // todo: fix getting qualifiers
    let tokenNames = [token.Name]

    let sourceFile = token.GetSourceFile()
    let coords = sourceFile.Document.GetCoordsByOffset(token.NameRange.EndOffset.Offset)
    let lineText = sourceFile.Document.GetLineText(coords.Line)

    // todo: provide tooltip for #r strings in fsx, should pass String tag
    let line = int coords.Line + 1
    let column = int coords.Column
    Some(results.CheckResults.GetToolTip(line, column, lineText, tokenNames, FSharpTokenTag.Identifier))

let richTextEscapeToHtml (text: RichText) =
    (RichText.Empty, text.GetFormattedParts()) ||> Seq.fold (fun result part ->
        result.Append(part.Text.Replace("<", "&lt;").Replace(">", "&gt;").Replace("\n", "<br>"), part.Style))

let asContent (text: RichText) =
    "<div class='content'>" + richTextEscapeToHtml text + "</div>"

//todo: move all the html related code to the platform
let asDefinition (text: RichText) =
    "<div class='definition'><pre style='word-wrap: break-word; white-space: pre-wrap;'>" +
    richTextEscapeToHtml text +
    "</pre></div>"

let createToolTip (header: RichText) (body: RichText) =
    if body.IsEmpty then
        asContent header
    else
        (asDefinition header).Append(body)

let presentLayouts (xmlDocService: FSharpXmlDocService) (context: ITreeNode) (ToolTipText layouts) =
    if layouts.IsEmpty then null else

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
                    [ match xmlDocService.GetXmlDoc(overload.XmlDoc, overload.Symbol, context.GetPsiModule()) with
                      | null -> ()
                      | xmlDocText -> yield xmlDocText.RichText

                      match overload.Remarks with
                      | Some remarks when not (isEmpty remarks) ->
                        yield remarks |> richText |> asContent
                      | _ -> () ]
                    |> richTextJoin "\n\n"

                createToolTip header body))
    |> richTextJoin IdentifierTooltipProvider.RIDER_TOOLTIP_SEPARATOR