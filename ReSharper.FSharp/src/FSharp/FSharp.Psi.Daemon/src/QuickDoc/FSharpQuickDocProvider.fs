module JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.QuickDoc

open FSharp.Compiler.EditorServices
open FSharp.Compiler.Text
open FSharp.Compiler.Tokenization
open JetBrains.Application.DataContext
open JetBrains.Application.Parts
open JetBrains.DocumentManagers
open JetBrains.ReSharper.Daemon.Tooltips
open JetBrains.ReSharper.Feature.Services.QuickDoc
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util.FcsTaggedText
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.RichText

module FSharpQuickDoc =
    let createKeywordTooltipText textTag text =
        RichText(text, toTextStyle textTag)

    let getKeywordTooltipText (token: IFSharpIdentifier) =
        let tokenType = token.GetTokenType()

        if tokenType == FSharpTokenType.KEYWORD_STRING_SOURCE_DIRECTORY then
            let sourceFile = token.FSharpFile.GetSourceFile()
            if isNull sourceFile then None else

            let path = sourceFile.Document.TryGetFilePath()
            if path.IsEmpty then None else

            createKeywordTooltipText TextTag.StringLiteral $"\"{path.Directory.FullPath}\""
            |> Some

        elif tokenType == FSharpTokenType.KEYWORD_STRING_SOURCE_FILE then
             token.FSharpFile.GetSourceFile()
             |> Option.ofObj
             |> Option.map (fun x -> createKeywordTooltipText TextTag.StringLiteral $"\"{x.Name}\"")

        elif tokenType == FSharpTokenType.KEYWORD_STRING_LINE then
            token.GetStartLine().Plus1().ToString()
            |> createKeywordTooltipText TextTag.NumericLiteral
            |> Some
        
        else None

    let getFSharpToolTipText (token: IFSharpIdentifier) : ToolTipText option =
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

    let getWildPatTooltipText (wildPat: IWildPat) =
        let fcsType = wildPat.TryGetFcsType()
        if isNull fcsType then None else

        Some(RichText(fcsType.Format(wildPat)))


type FSharpQuickDocPresenter(xmlDocService: FSharpXmlDocService, node: ITreeNode) =
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

    let createWildPatTooltip (wildPat: IWildPat) =
        FSharpQuickDoc.getWildPatTooltipText wildPat
        |> Option.map asDefinition
        |> Option.defaultValue null

    let createIdentifierTooltip (identifier: IFSharpIdentifier) =
        match FSharpQuickDoc.getKeywordTooltipText identifier with
        | Some keywordText -> asDefinition keywordText
        | None ->

        FSharpQuickDoc.getFSharpToolTipText identifier
        |> Option.map (fun (ToolTipText layouts) ->
            if layouts.IsEmpty then null else

            layouts |> List.collect (function
                | ToolTipElement.None -> []
                | ToolTipElement.CompositionError errorText -> [ RichText(errorText) ]

                | ToolTipElement.Group(overloads) ->
                    overloads |> List.map (fun overload ->
                        let header =
                            [ if not overload.MainDescription.IsEmpty then
                                yield overload.MainDescription |> ofFcsRichText

                              if not overload.TypeMapping.IsEmpty then
                                yield overload.TypeMapping |> List.map ofFcsRichText |> richTextJoin "\n" ]
                            |> richTextJoin "\n\n"
                            |> asDefinition

                        let body =
                            [ match xmlDocService.GetXmlDoc(overload.XmlDoc, overload.Symbol, identifier.GetPsiModule()) with
                              | null -> ()
                              | xmlDocText -> yield xmlDocText.RichText

                              match overload.Remarks with
                              | Some remarks when not remarks.IsEmpty ->
                                yield remarks |> ofFcsRichText |> asContent
                              | _ -> () ]
                            |> richTextJoin "\n\n"

                        header.Append(body)))
            |> richTextJoin IdentifierTooltipProvider.RIDER_TOOLTIP_SEPARATOR
        )
        |> Option.defaultValue null

    member x.CreateRichTextTooltip() =
        match node with
        | :? IFSharpIdentifier as identifier -> createIdentifierTooltip identifier
        | :? IWildPat as wildPat -> createWildPatTooltip wildPat
        | _ -> null

    interface IQuickDocPresenter with
        member this.GetHtml _ =
            QuickDocTitleAndText(this.CreateRichTextTooltip(), null)

        member this.GetId() = null
        member this.OpenInEditor _ = ()
        member this.ReadMore _ = ()
        member this.Resolve _ = null


[<QuickDocProvider(-1000)>]
type FSharpQuickDocProvider(xmlDocService: FSharpXmlDocService) =
    let tryFindNode (context: IDataContext) : ITreeNode =
        let sourceFile = context.GetData(PsiDataConstants.SOURCE_FILE)
        if isNull sourceFile then null else

        context.GetData(PsiDataConstants.SELECTED_TREE_NODES)
        |> Option.ofObj
        |> Option.defaultValue []
        |> Seq.choose (function
            | :? IFSharpIdentifier as node when node.GetSourceFile() = sourceFile ->
                let activePatternCaseName = ActivePatternCaseNameNavigator.GetByIdentifier(node)
                let activePatternId = ActivePatternIdNavigator.GetByCase(activePatternCaseName)
                if isNotNull activePatternId then Some(activePatternId :> ITreeNode) else Some node
            | node when node.GetTokenType() == FSharpTokenType.UNDERSCORE ->
                match node.Parent with
                | :? IDotLambdaId
                | :? IWildPat -> Some node.Parent
                | _ -> None
            | _ -> None
        )
        |> Seq.tryExactlyOne
        |> Option.toObj

    interface IQuickDocProvider with
        member this.CanNavigate(context) =
            match tryFindNode context with
            | :? IFSharpIdentifier as token ->
                FSharpQuickDoc.getKeywordTooltipText token |> Option.isSome ||

                FSharpQuickDoc.getFSharpToolTipText token
                |> Option.map (fun (ToolTipText layouts) -> not layouts.IsEmpty)
                |> Option.defaultValue false
            | :? IWildPat as wildPat -> FSharpQuickDoc.getWildPatTooltipText wildPat |> Option.isSome
            | _ -> false

        member this.Resolve(context, resolved) =
            let node = tryFindNode context
            if isNotNull node then
                resolved.Invoke(FSharpQuickDocPresenter(xmlDocService, node), FSharpLanguage.Instance)


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
