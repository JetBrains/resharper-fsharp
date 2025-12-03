namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Resources
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<AutoOpen>]
module private Utils =
    let createLineComment text = FSharpComment.CreateLineComment(" " + text)


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
        createLineComment $"{ReSharperControlConstruct.DisableOncePrefix} {severityId}"

    override this.RestoreNode = null


type DisableAndRestoreWarningAction(highlightingRanges, file, severityId) =
    inherit DisableWarningActionBase(highlightingRanges, file)
    override this.Text = Strings.FSharpDisableAndRestoreWithComments_Text

    override this.DisableNode =
        createLineComment $"{ReSharperControlConstruct.DisablePrefix} {severityId}"

    override this.RestoreNode =
        createLineComment $"{ReSharperControlConstruct.RestorePrefix} {severityId}"


type DisableWarningInFileAction(file: IFSharpFile, severityId) =
    inherit ContextActionBase()
    let disableAll =
        severityId = ReSharperControlConstruct.DisableAllReSharperWarningsID

    let findPlaceToInsert (file: IFSharpFile) severityId =
        let rec findLastNode (prevNode: ITreeNode) (node: ITreeNode) severityId =
            match node with
            | :? Whitespace
            | :? NewLine -> findLastNode prevNode node.NextSibling severityId
            | :? FSharpComment as comment ->
                if comment.CommentType = CommentType.DocComment then prevNode, true else

                let controlConstruct = ReSharperControlConstruct.ParseCommentText(comment.CommentText)
                if not controlConstruct.IsDisable then prevNode, true else

                if severityId > controlConstruct.GetControlIdsText() then
                    findLastNode node.NextSibling node.NextSibling severityId
                else prevNode, false

            | _ -> prevNode, true

        let firstModule = file.ModuleDeclarationsEnumerable |> Seq.head
        let firstNode =
            match firstModule with
            | :? IAnonModuleDeclaration as firstModule -> firstModule.FirstChild
            | _ -> file.FirstChild

        let node, needsAdditionalNewLine = findLastNode firstNode firstNode severityId
        node, needsAdditionalNewLine

    let removeExistingComments (file: IFSharpFile) =
        let candidatesToRemove = ResizeArray()

        for comment in file.Descendants<FSharpComment>() do
            let info = ReSharperControlConstruct.ParseCommentText(comment.CommentText)
            if info.IsRecognized then candidatesToRemove.Add((comment, info))

        for comment, info in candidatesToRemove do
            if disableAll then deleteChild comment else

            let diagnosticIds = info.GetControlIds() |> LocalList
            if not (diagnosticIds.Contains(severityId)) then () else

            if diagnosticIds.Count = 1 then deleteChild comment else

            diagnosticIds.Remove(severityId) |> ignore
            let diagnosticIds = diagnosticIds.ToArray() |> String.concat ", "

            ReSharperControlConstruct.CreateText(info.Kind, diagnosticIds)
            |> createLineComment
            |> replace comment

    override this.Text =
        if disableAll then Strings.FSharpDisableAllInspectionsInFile_Text
        else Strings.FSharpDisableInFileWithComment_Text

    override this.IsAvailable _ = isValid file

    override this.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(file.IsPhysical())

        removeExistingComments file
        let nodeToInsertBefore, needsAdditionalNewLine = findPlaceToInsert file severityId

        let commentNode = createLineComment $"{ReSharperControlConstruct.DisablePrefix} {severityId}"

        let commentNode = ModificationUtil.AddChildBefore(nodeToInsertBefore, commentNode)
        if needsAdditionalNewLine then commentNode.AddLineBreakAfter(minLineBreaks = 2) |> ignore

        null
