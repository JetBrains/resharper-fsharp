namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Resources
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<AbstractClass>]
type DisableWarningActionBase(highlightingRanges: DocumentRange[], file: IFSharpFile) =
    inherit ContextActionBase()

    let rec skipParentsWithSameOffset (node: ITreeNode) checkStartOffset =
        match node.Parent with
        | null
        | :? IAnonModuleDeclaration
        | :? IFile -> node
        | parent ->

        let equals =
            if checkStartOffset then parent.GetDocumentStartOffset() = node.GetDocumentStartOffset()
            else parent.GetDocumentEndOffset() = node.GetDocumentEndOffset()

        if equals then skipParentsWithSameOffset parent checkStartOffset else node

    abstract member DisableNode: ITreeNode
    abstract member RestoreNode: ITreeNode

    override this.IsAvailable _ =
        isValid file && highlightingRanges |> Seq.forall (fun x -> x.IsValid())

    override this.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(file.IsPhysical())

        let highlightingRanges = highlightingRanges |> Array.sortBy _.StartOffset.Offset

        let firstRange = highlightingRanges[0]
        let firstNode = file.FindNodeAt(firstRange)

        let lastRange = highlightingRanges |> Array.last
        let lastNode = file.FindNodeAt(lastRange)

        let document = firstRange.Document

        match this.DisableNode with
        | null -> null
        | openNode ->

        let offset = document.GetLineStartDocumentOffset(firstNode.StartLine)
        let nodeToAddBefore = JetBrains.ReSharper.Psi.Impl.CodeStyle.FormatterImplHelper.SkipRightWhitespaces(file.FindNodeAt(offset + 1))
        let nodeToAddBefore = skipParentsWithSameOffset nodeToAddBefore true
        addNodeBefore nodeToAddBefore openNode

        match this.RestoreNode with
        | null -> null
        | closeNode ->

        let offset = document.GetLineEndDocumentOffsetNoLineBreak(lastNode.EndLine)
        let nodeToAddAfter = file.FindNodeAt(offset)

        let nodeToAddAfter = skipParentsWithSameOffset nodeToAddAfter false
        addNodeAfter nodeToAddAfter closeNode

        null


type DisableWarningOnceAction(highlightingRanges, file, severityId) =
    inherit DisableWarningActionBase(highlightingRanges, file)
    override this.Text = Strings.FSharpDisableOnceWithComment_Text

    override this.DisableNode =
        FSharpComment(FSharpTokenType.LINE_COMMENT, $"// {ReSharperControlConstruct.DisableOncePrefix} {severityId}")

    override this.RestoreNode = null


type DisableAndRestoreWarningAction(highlightingRanges, file, severityId) =
    inherit DisableWarningActionBase(highlightingRanges, file)
    override this.Text = Strings.FSharpDisableAndRestoreWithComments_Text

    override this.DisableNode =
        FSharpComment(FSharpTokenType.LINE_COMMENT, $"// {ReSharperControlConstruct.DisablePrefix} {severityId}")

    override this.RestoreNode =
        FSharpComment(FSharpTokenType.LINE_COMMENT, $"// {ReSharperControlConstruct.RestorePrefix} {severityId}")


type DisableWarningInFileAction(file: IFSharpFile, severityId) =
    inherit ContextActionBase()
    let disableAll =
        severityId = ReSharperControlConstruct.DisableAllReSharperWarningsID

    let findInsertRange (file: IFSharpFile) severityId =
        let rec findLastNode (prevNode: ITreeNode) (node: ITreeNode) severityId =
            match node with
            | :? Whitespace
            | :? NewLine -> findLastNode node node.NextSibling severityId
            | :? FSharpComment as comment ->
                if comment.CommentType = CommentType.DocComment then prevNode, true else

                let controlConstruct = ReSharperControlConstruct.ParseCommentText(comment.CommentText)
                if not controlConstruct.IsDisable then prevNode, true else

                if severityId > controlConstruct.GetControlIdsText() then
                    findLastNode node node.NextSibling severityId
                else prevNode, false

            | _ -> prevNode, true

        let firstModule = file.ModuleDeclarationsEnumerable |> Seq.head
        let firstNode =
            match firstModule with
            | :? IAnonModuleDeclaration as firstModule -> firstModule.FirstChild
            | _ -> file.FirstChild

        let lastNode, needsAdditionalNewLine = findLastNode firstNode firstNode severityId
        firstNode, lastNode, needsAdditionalNewLine

    override this.Text =
        if disableAll then Strings.FSharpDisableAllInspectionsInFile_Text
        else Strings.FSharpDisableInFileWithComment_Text

    override this.IsAvailable _ = isValid file

    override this.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(file.IsPhysical())

        let firstNode, lastNode, needsAdditionalNewLine = findInsertRange file severityId

        let commentNode =
            FSharpComment(FSharpTokenType.LINE_COMMENT, $"// {ReSharperControlConstruct.DisablePrefix} {severityId}")

        if firstNode == lastNode then
            let commentNode = ModificationUtil.AddChildBefore(firstNode, commentNode)
            if needsAdditionalNewLine then
                commentNode.AddLineBreakAfter(minLineBreaks = 2) |> ignore
        else
            let commentNode = ModificationUtil.AddChildAfter(lastNode, commentNode)
            if needsAdditionalNewLine then
                commentNode.AddLineBreakAfter(minLineBreaks = 2) |> ignore

            if disableAll then
                deleteChildRange firstNode lastNode

        null
