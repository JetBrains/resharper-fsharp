namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Resources
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
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
        isValid file && highlightingRanges |> Seq.forall _.IsValid()

    override this.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(file.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let highlightingRanges = highlightingRanges |> Array.sortBy _.StartOffset.Offset
        let lineEnding = file.GetLineEnding()

        let firstRange = highlightingRanges[0]
        let firstNode = file.FindNodeAt(firstRange)

        let lastRange = highlightingRanges |> Array.last
        let lastNode = file.FindNodeAt(lastRange)

        let document = firstRange.Document

        match this.DisableNode with
        | null -> null
        | openNode ->
        let offset = document.GetLineStartDocumentOffset(firstNode.StartLine)
        let nodeToAddBefore = file.FindNodeAt(offset + 1)
        let indent = if nodeToAddBefore :? Whitespace then nodeToAddBefore.GetTextLength() else 0

        let nodeToAddBefore = skipParentsWithSameOffset nodeToAddBefore true
        addNodesBefore nodeToAddBefore [
            if indent > 0 then Whitespace(indent)
            openNode
            NewLine(lineEnding)
        ] |> ignore

        match this.RestoreNode with
        | null -> null
        | closeNode ->
        let offset = document.GetLineEndDocumentOffsetNoLineBreak(lastNode.EndLine)
        let nodeToAddAfter = file.FindNodeAt(offset)

        let nodeToAddAfter = skipParentsWithSameOffset nodeToAddAfter false
        addNodesAfter nodeToAddAfter [
            NewLine(lineEnding)
            if indent > 0 then Whitespace(indent)
            closeNode
        ] |> ignore

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
        use disableFormatter = new DisableCodeFormatter()

        let lineEnding = file.GetLineEnding()
        let firstNode, lastNode, needsAdditionalNewLine = findInsertRange file severityId

        let treeNodes: ITreeNode list =
            [
                FSharpComment(FSharpTokenType.LINE_COMMENT, $"// {ReSharperControlConstruct.DisablePrefix} {severityId}")
                NewLine(lineEnding)
                if needsAdditionalNewLine then NewLine(lineEnding)
            ]
        if firstNode == lastNode then
            addNodesBefore firstNode treeNodes |> ignore
        else
            addNodesAfter lastNode treeNodes |> ignore
            if disableAll then deleteChildRange firstNode lastNode
        null
