module JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.QuickDoc

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Tokenization
open JetBrains.Application.DataContext
open JetBrains.DocumentModel.DataContext
open JetBrains.ReSharper.Daemon
open JetBrains.ReSharper.Feature.Services.QuickDoc
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.UI.RichText
open JetBrains.Util
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Plugins.FSharp.Util.FcsTaggedText
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Modules

// Note:
// I made it work based on tokens rather than declared elements
// in order to minimize the changes compared to FSharpIdentifierTooltipProvider.
// If you need to improve F# quickdocs and feel like this API is too restrictive,
// making it use declared elements would be the best first step.
// See QuickDocTypeMemberPresenter for inspiration. 
type FSharpQuickDocPresenter(xmlDocService: FSharpXmlDocService, file: IFSharpFile, token) =
    let [<Literal>] opName = "FSharpQuickDocProvider"
    let richTextEscapeToHtml (text: RichText) =
        (RichText.Empty, text.GetFormattedParts()) ||> Seq.fold (fun result part ->
            result.Append(part.Text.Replace("<", "&lt;").Replace(">", "&gt;").Replace("\n", "<br>"), part.Style))
    let asContent (text: RichText) = "<div class='content'>" + (richTextEscapeToHtml text) + "</div>"
    //todo: move all the html related code to the platform
    let asDefinition (text: RichText) = "<div class='definition'><pre style='word-wrap: break-word; white-space: pre-wrap;'>" + (richTextEscapeToHtml text) + "</pre></div>"
    let createToolTip (header: RichText) (body: RichText) =
        if body.IsEmpty then asContent header else
        (asDefinition header).Append(body)
    static member GetFSharpToolTipText(checkResults: FSharpCheckFileResults, token: IFSharpIdentifierToken) =
        // todo: fix getting qualifiers
        let tokenNames = [token.Name]

        let sourceFile = token.GetSourceFile()
        let coords = sourceFile.Document.GetCoordsByOffset(token.GetTreeEndOffset().Offset)
        let lineText = sourceFile.Document.GetLineText(coords.Line)
        use cookie = CompilationContextCookie.GetOrCreate(sourceFile.GetPsiModule().GetContextFromModule())

        // todo: provide tooltip for #r strings in fsx, should pass String tag
        checkResults.GetToolTip(int coords.Line + 1, int coords.Column, lineText, tokenNames, FSharpTokenTag.Identifier)
    member x.CreateRichTextTooltip() =
        match file.GetParseAndCheckResults(true, opName) with
            | None -> RichText()
            | Some results ->
                let (ToolTipText layouts) = FSharpQuickDocPresenter.GetFSharpToolTipText(results.CheckResults, token)
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

    interface IQuickDocPresenter with
        member this.GetHtml _ =
            let richText = this.CreateRichTextTooltip()
            QuickDocTitleAndText(richText, "TODO: add title")

        // TODO: return something that depends on constructor arguments
        member this.GetId() = "F# element ID for frontend-backend interop"
        // TODO: implement this to make 'edit source' work. Not useful until there's proper navigation
        member this.OpenInEditor _ = ()
        // lol who cares
        // read more - shmread shmore
        member this.ReadMore _ = ()
        // TODO: Implement this along with adding links to quickdocs to make navigation work 
        member this.Resolve _ = null

[<QuickDocProvider(-1000)>]
type FSharpQuickDocProvider(xmlDocService: FSharpXmlDocService) =
    member private x.tryFindFSharpFile(context: IDataContext) =
        match context.GetData(DocumentModelDataConstants.EDITOR_CONTEXT) with
        | null -> null
        | editorContext ->
        let offset = editorContext.CaretOffset
        match context.GetData(PsiDataConstants.SOURCE_FILE) with
        | null -> null
        | sourceFile ->
        sourceFile.GetPsiFile(offset).AsFSharpFile()
        
    
    
    interface IQuickDocProvider with
        member this.CanNavigate(context) =
            this.tryFindFSharpFile context != null

        member this.Resolve(context, resolved) =
            let file = this.tryFindFSharpFile context
            let offset = context.GetData(DocumentModelDataConstants.EDITOR_CONTEXT).CaretOffset
            match file.FindTokenAt(TreeOffset(offset.Offset)).As<IFSharpIdentifierToken>() with
            | null -> ()
            | token ->
            resolved.Invoke(FSharpQuickDocPresenter(xmlDocService, file, token), FSharpLanguage.Instance)
            
    