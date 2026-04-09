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

[<Interface>]
type IDiagnosticId =
    abstract member SourceName: string

type ReSharperDiagnosticId = ReSharperDiagnosticId of diagnosticId: string with
    member x.SourceName =
        let (ReSharperDiagnosticId(diagnosticId)) = x
        diagnosticId

    interface IDiagnosticId with
        member x.SourceName = x.SourceName

type CompilerDiagnosticId(sourceName: string) =
    let normalize (s: string) = s.Trim().Trim('"').TrimStart('F', 'S', '0')

    member _.SourceName = sourceName

    interface IDiagnosticId with
        member x.SourceName = x.SourceName

    override x.Equals(obj) =
        match obj with
        | :? CompilerDiagnosticId as diagnosticId ->
            normalize x.SourceName = normalize diagnosticId.SourceName
        | _ ->
            false

    override this.GetHashCode() = (normalize sourceName).GetHashCode()

type Warning =
    | ReSharper of diagnosticId: ReSharperDiagnosticId
    | Compiler of diagnosticId: CompilerDiagnosticId

    member x.DiagnosticId: IDiagnosticId =
        match x with
        | ReSharper severityId -> severityId
        | Compiler compilerId -> compilerId

[<AutoOpen>]
module private Utils =
    let createLineComment text = FSharpComment.CreateLineComment(" " + text)

    type DirectiveType =
        | Disable
        | DisableOnce
        | Restore

    type DiagnosticDirective = {
            Type: DirectiveType
            Node: ITreeNode
            DiagnosticIds: IDiagnosticId seq
        }

    [<Interface>]
    type IDiagnosticDirectivePresenter =
        abstract member Parse: ITreeNode -> DiagnosticDirective option
        abstract member CreateDirective: IFSharpFile * DirectiveType * string seq -> ITreeNode

    type ReSharperDirectivePresenter private () =

        let createDirective kind diagnosticIds =
            ReSharperControlConstruct.CreateText(kind, diagnosticIds |> String.concat ", ")
            |> createLineComment

        static member val Instance = ReSharperDirectivePresenter()

        interface IDiagnosticDirectivePresenter with
            member x.Parse(node) =
               match node with
               | :? FSharpComment as comment ->
                   let rcc = ReSharperControlConstruct.ParseCommentText(comment.CommentText)
                   if rcc.IsRecognized then
                       Some { Type =
                                match rcc.Kind with
                                | ReSharperControlConstruct.Kind.Disable -> Disable
                                | ReSharperControlConstruct.Kind.DisableOnce -> DisableOnce
                                | _ -> Restore
                              Node = comment
                              DiagnosticIds = rcc.GetControlIds() |> Seq.map (fun x -> ReSharperDiagnosticId(x)) }
                   else None
               | _ -> None
    
            member x.CreateDirective(_, directiveType, diagnosticIds) =
                match directiveType with
                | Disable -> createDirective ReSharperControlConstruct.Kind.Disable diagnosticIds
                | DisableOnce -> createDirective ReSharperControlConstruct.Kind.DisableOnce diagnosticIds
                | Restore -> createDirective ReSharperControlConstruct.Kind.Restore diagnosticIds

    type CompilerDirectivePresenter private () =
        
        let createDirective (file: IFSharpFile) directive diagnosticIds =
            let factory = file.CreateElementFactory()
            factory.CreateHashDirective(directive, diagnosticIds)

        static member val Instance = CompilerDirectivePresenter()

        interface IDiagnosticDirectivePresenter with
            member x.Parse(node) =
                match node with
                | :? IWarningDirective as directive ->
                    Some { Type = if directive.IsNowarn() then Disable else Restore
                           Node = directive
                           DiagnosticIds = directive.ArgsEnumerable |> Seq.map (fun x -> CompilerDiagnosticId(x.GetText())) }
                | _ -> None
    
            member x.CreateDirective(file, directiveType, diagnosticIds) =
                match directiveType with
                | Disable -> createDirective file "nowarn" diagnosticIds
                | DisableOnce -> null
                | Restore -> createDirective file "warnon" diagnosticIds
    
    let createDirectivePresenter diagnosticId: IDiagnosticDirectivePresenter =
        match diagnosticId with
        | ReSharper _ -> ReSharperDirectivePresenter.Instance
        | Compiler _ -> CompilerDirectivePresenter.Instance


[<AbstractClass>]
type DisableWarningActionBase(highlightingRanges: DocumentRange[],
                              file: IFSharpFile,
                              warning: Warning) =
    inherit ContextActionBase()

    let presenter = createDirectivePresenter warning

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
    default x.DisableNode = presenter.CreateDirective(file, Disable, [warning.DiagnosticId.SourceName])

    abstract member RestoreNode: ITreeNode
    default x.RestoreNode = presenter.CreateDirective(file, Restore, [warning.DiagnosticId.SourceName])

    member private x.Presenter = presenter

    override this.IsAvailable _ =
        isValid file && highlightingRanges |> Seq.forall _.IsValid()

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
        ModificationUtil.AddChildBefore(nodeToAddBefore, openNode).FormatNode()

        match this.RestoreNode with
        | null -> null
        | closeNode ->

        let offset = document.GetLineEndDocumentOffsetNoLineBreak(lastNode.EndLine)
        let nodeToAddAfter = file.FindNodeAt(offset)

        let nodeToAddAfter = skipParentsWithSameOffset nodeToAddAfter false
        ModificationUtil.AddChildAfter(nodeToAddAfter, closeNode).FormatNode()

        null


type DisableWarningOnceAction(highlightingRanges, file, warning: ReSharperDiagnosticId) =
    inherit DisableWarningActionBase(highlightingRanges, file, ReSharper(warning))
    let presenter: IDiagnosticDirectivePresenter = ReSharperDirectivePresenter.Instance
    override this.Text = Strings.FSharpDisableOnceWithComment_Text
    override this.DisableNode = presenter.CreateDirective(file, DisableOnce, [warning.SourceName])
    override this.RestoreNode = null

type DisableAndRestoreWarningAction(highlightingRanges, file, warning) =
    inherit DisableWarningActionBase(highlightingRanges, file, warning)
    override this.Text =
        match warning with
        | ReSharper _ -> Strings.FSharpDisableAndRestoreWithComments_Text
        | Compiler _ -> Strings.FSharpDisableAndRestoreWithDirectives_Text

type DisableWarningInFileAction(file: IFSharpFile, warning) =
    inherit DisableWarningActionBase([||], file, warning)

    let disableAll =
        match warning with
        | ReSharper severityId -> severityId.SourceName = ReSharperControlConstruct.DisableAllReSharperWarningsID
        | _ -> false
    
    let findPlaceToInsert (file: IFSharpFile) (engine: IDiagnosticDirectivePresenter) =
        let rec findLastNode (prevNode: ITreeNode) (node: ITreeNode) (engine: IDiagnosticDirectivePresenter) =
            match node with
            | :? Whitespace
            | :? NewLine -> findLastNode prevNode node.NextSibling engine

            | :? IWarningDirective when (warning.DiagnosticId :? ReSharperDiagnosticId) ->
                findLastNode node.NextSibling node.NextSibling engine

            | :? IWarningDirective | :? FSharpComment as directive ->
                match engine.Parse(directive) with
                | None -> prevNode, true
                | Some directive ->
                    if directive.Type = Restore then prevNode, true else

                    let firstDiagnosticId =
                        directive.DiagnosticIds
                        |> Seq.tryHead
                        |> Option.map _.SourceName
                        |> Option.defaultValue ""

                    if warning.DiagnosticId.SourceName > firstDiagnosticId then
                        findLastNode node.NextSibling node.NextSibling engine
                    
                    else prevNode, false

            | _ -> prevNode, true

        let firstModule = file.ModuleDeclarationsEnumerable |> Seq.head
        let firstNode =
            match firstModule with
            | :? IAnonModuleDeclaration as firstModule -> firstModule.FirstChild
            | _ -> file.FirstChild

        let node, needsAdditionalNewLine = findLastNode firstNode firstNode engine
        node, needsAdditionalNewLine

    let removeExistingDirectives (presenter: IDiagnosticDirectivePresenter) =
        let directives =
            [| for comment in file.Descendants() do
                   match presenter.Parse(comment) with
                   | Some directive -> directive
                   | None -> () |]

        for directive in directives do
            if disableAll then deleteChild directive.Node else

            let diagnosticIds = directive.DiagnosticIds |> ResizeArray
            diagnosticIds.RemoveAll((=) warning.DiagnosticId) |> ignore
            if diagnosticIds.Count = 0 then deleteChild directive.Node else

            let diagnosticIds = diagnosticIds |> Seq.map _.SourceName
            let node = presenter.CreateDirective(file, directive.Type, diagnosticIds)

            replace directive.Node node

    override this.Text =
        match warning with
        | ReSharper _ ->
            if disableAll then Strings.FSharpDisableAllInspectionsInFile_Text
            else Strings.FSharpDisableInFileWithComment_Text
        | Compiler _ ->
            Strings.FSharpDisableInFileWithDirective_Text

    override this.IsAvailable _ = isValid file

    override this.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(file.IsPhysical())

        let presenter = createDirectivePresenter warning
        removeExistingDirectives presenter
        let nodeToInsertBefore, needsAdditionalNewLine =  findPlaceToInsert file presenter

        let disableNode = ModificationUtil.AddChildBefore(nodeToInsertBefore, this.DisableNode)
        if needsAdditionalNewLine then disableNode.AddLineBreakAfter(minLineBreaks = 2) |> ignore

        null
