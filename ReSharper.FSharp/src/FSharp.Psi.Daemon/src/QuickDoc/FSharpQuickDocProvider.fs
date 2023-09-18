module JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.QuickDoc

open FSharp.Compiler.EditorServices
open JetBrains.Application.DataContext
open JetBrains.DocumentModel.DataContext
open JetBrains.ReSharper.Feature.Services.QuickDoc
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.Files

type FSharpQuickDocPresenter(xmlDocService: FSharpXmlDocService, identifier: IFSharpIdentifier) =
    member x.CreateRichTextTooltip() =
        FSharpQuickDoc.getFSharpToolTipText identifier
        |> Option.map (FSharpQuickDoc.presentLayouts xmlDocService identifier)
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
        |> Seq.choose (function
            | :? IFSharpIdentifier as node when node.GetSourceFile() = sourceFile ->
                let activePatternCaseName = ActivePatternCaseNameNavigator.GetByIdentifier(node)
                let activePatternId = ActivePatternIdNavigator.GetByCase(activePatternCaseName)
                if isNotNull activePatternId then Some (activePatternId :> IFSharpIdentifier) else Some node
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
