module JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.QuickDoc

open FSharp.Compiler.EditorServices
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text
open FSharp.Compiler.Tokenization
open JetBrains.Application.DataContext
open JetBrains.Application.Parts
open JetBrains.DocumentManagers
open JetBrains.DocumentModel.DataContext
open JetBrains.ReSharper.Daemon.Tooltips
open JetBrains.ReSharper.Feature.Services.QuickDoc
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util.FcsTaggedText
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.RichText

module FSharpQuickDoc =
    let createTextTooltipText textTag text =
        ToolTipText([ToolTipElement.Single([|TaggedText(textTag, text)|], FSharpXmlDoc.None)])

    let getKeywordTooltipText (token: IFSharpIdentifier) =
        let tokenType = token.GetTokenType()

        if tokenType == FSharpTokenType.KEYWORD_STRING_SOURCE_DIRECTORY then
            let sourceFile = token.FSharpFile.GetSourceFile()
            if isNull sourceFile then None else
            let path = sourceFile.Document.TryGetFilePath()
            if path.IsEmpty then None else
            createTextTooltipText TextTag.StringLiteral $"\"{path.Directory.FullPath}\"" |> Some

        elif tokenType == FSharpTokenType.KEYWORD_STRING_SOURCE_FILE then
             token.FSharpFile.GetSourceFile()
             |> Option.ofObj
             |> Option.map (fun x -> $"\"{x.Name}\"")
             |> Option.map (createTextTooltipText TextTag.StringLiteral)

        elif tokenType == FSharpTokenType.KEYWORD_STRING_LINE then
            token.GetStartLine().Plus1().ToString()
            |> createTextTooltipText TextTag.NumericLiteral
            |> Some
        
        else None

    let getFSharpToolTipText (token: IFSharpIdentifier) : ToolTipText option =
        match getKeywordTooltipText token with
        | Some _ as text -> text
        | None ->

        match token.FSharpFile.GetParseAndCheckResults(true, "FSharpQuickDoc") with
        | None -> None
        | Some results ->

        // todo: fix getting qualifiers
        let tokenNames = [token.Name]

        let sourceFile = token.GetSourceFile()
        let coords = token.FSharpFile.GetDocumentOffset(token.NameRange.EndOffset).ToDocumentCoords()
        let lineText = sourceFile.Document.GetLineText(coords.Line)

        // todo: provide tooltip for #r strings in fsx, should pass String tag
        let line = int coords.Line + 1
        let column = int coords.Column
        Some(results.CheckResults.GetToolTip(line, column, lineText, tokenNames, FSharpTokenTag.Identifier))


type FSharpQuickDocPresenter(xmlDocService: FSharpXmlDocService, identifier: IFSharpIdentifier) =
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

    member x.CreateRichTextTooltip() =
        FSharpQuickDoc.getFSharpToolTipText identifier
        |> Option.map (fun (ToolTipText layouts) ->
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
                            [ match xmlDocService.GetXmlDoc(overload.XmlDoc, overload.Symbol, identifier.GetPsiModule()) with
                              | null -> ()
                              | xmlDocText -> yield xmlDocText.RichText

                              match overload.Remarks with
                              | Some remarks when not (isEmpty remarks) ->
                                yield remarks |> richText |> asContent
                              | _ -> () ]
                            |> richTextJoin "\n\n"

                        createToolTip header body))
            |> richTextJoin IdentifierTooltipProvider.RIDER_TOOLTIP_SEPARATOR
        )
        |> Option.defaultValue null

    interface IQuickDocPresenter with
        member this.GetHtml _ =
            QuickDocTitleAndText(this.CreateRichTextTooltip(), null)

        member this.GetId() = null
        member this.OpenInEditor _ = ()
        member this.ReadMore _ = ()
        member this.Resolve _ = null


[<QuickDocProvider(-1000)>]
type FSharpQuickDocProvider(xmlDocService: FSharpXmlDocService) =
    let tryFindFSharpFile (context: IDataContext) =
        let editorContext = context.GetData(DocumentModelDataConstants.EDITOR_CONTEXT)
        if isNull editorContext then null else

        let sourceFile = context.GetData(PsiDataConstants.SOURCE_FILE)
        if isNull sourceFile then null else

        sourceFile.GetPsiFile(editorContext.CaretOffset).AsFSharpFile()

    let tryFindToken (context: IDataContext) : IFSharpIdentifier option =
        let sourceFile = context.GetData(PsiDataConstants.SOURCE_FILE)
        if isNull sourceFile then None else

        context.GetData(PsiDataConstants.SELECTED_TREE_NODES)
        |> Option.ofObj
        |> Option.defaultValue []
        |> Seq.choose (function
            | :? IFSharpIdentifier as node when node.GetSourceFile() = sourceFile ->
                let activePatternCaseName = ActivePatternCaseNameNavigator.GetByIdentifier(node)
                let activePatternId = ActivePatternIdNavigator.GetByCase(activePatternCaseName)
                if isNotNull activePatternId then Some (activePatternId :> IFSharpIdentifier) else Some node
            | node when node.GetTokenType() == FSharpTokenType.UNDERSCORE ->
                match node.Parent with
                | :? IDotLambdaId as dotLambdaId -> Some dotLambdaId
                | _ -> None
            | _ -> None
        )
        |> Seq.tryExactlyOne

    interface IQuickDocProvider with
        member this.CanNavigate(context) =
            tryFindToken context
            |> Option.bind FSharpQuickDoc.getFSharpToolTipText
            |> Option.map (fun (ToolTipText layouts) -> not layouts.IsEmpty)
            |> Option.defaultValue false

        member this.Resolve(context, resolved) =
            tryFindToken context
            |> Option.iter (fun token ->
                resolved.Invoke(FSharpQuickDocPresenter(xmlDocService, token :?> _), FSharpLanguage.Instance))


[<Language(typeof<FSharpLanguage>, Instantiation.DemandAnyThreadSafe)>]
type FSharpShouldNotSuppressUndeclaredElements() =
    interface IShouldNotSuppressUndeclaredElements with
        member _.Check(file, offset) =
            match file.FindNodeAt(TreeOffset(offset)) with
            | :? IFSharpIdentifier as identifier ->
                let tokenType = identifier.GetTokenType()
                if tokenType == FSharpTokenType.KEYWORD_STRING_SOURCE_DIRECTORY ||
                   tokenType == FSharpTokenType.KEYWORD_STRING_SOURCE_FILE ||
                   tokenType == FSharpTokenType.KEYWORD_STRING_LINE then true else

                let referenceOwner = identifier |> FSharpReferenceOwnerNavigator.GetByIdentifier

                // Covers the erased provided types and its members
                match referenceOwner with
                | :? IReferenceExpr
                | :? ITypeReferenceName -> referenceOwner.Reference.HasFcsSymbol
                | _ -> false

            | _ -> false
