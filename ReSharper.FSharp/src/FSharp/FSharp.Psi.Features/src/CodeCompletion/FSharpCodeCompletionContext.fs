namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open System
open System.Linq
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Symbols
open JetBrains.Application.Parts
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Threading
open JetBrains.Util
open JetBrains.Util.Concurrency
open JetBrains.Util.Extension
open JetBrains.Util.Logging

type FcsCodeCompletionContext =
    { PartialName: PartialLongName
      CompletionContext: CompletionContext option
      LineText: string
      Coords: DocumentCoords
      mutable DisplayContext: FSharpDisplayContext }

    static member Invalid =
        { PartialName = PartialLongName.Empty(-1)
          CompletionContext = None
          LineText = ""
          Coords = DocumentCoords.Empty
          DisplayContext = Unchecked.defaultof<_> }

type FSharpReparseContext(fsFile: IFSharpFile, treeTextRange: TreeTextRange) =
    let [<Literal>] moniker = "F# reparse context"

    interface IReparseContext with
        member this.GetReparseResult(newText) =
            // todo: remove standalone document, try to make it incremental
            let source = fsFile.GetText().Insert(treeTextRange.StartOffset.Offset, newText)
            let documentFactory = Shell.Instance.GetComponent<IInMemoryDocumentFactory>()
            let document = documentFactory.CreateSimpleDocumentFromText(source, moniker)

            // todo: reparse as sig?
            let parser = fsFile.GetFSharpLanguageService().CreateParser(document, fsFile.GetSourceFile(), FSharpProjectFileType.FsExtension)
            let newFile =
                try
                    let newFile =
                        parser.ParseFSharpFile(true,
                            StandaloneDocument = document,
                            ResolvedSymbolsCache = fsFile.ResolvedSymbolsCache,
                            DocumentRangeTranslator = IdenticalDocumentRangeTranslator(document))
                    SandBox.CreateSandBoxFor(newFile, fsFile.GetPsiModule())
                    newFile
                with exn ->
                    if not (exn.IsOperationCanceled()) then
                        Logger.GetLogger<FSharpReparseContext>().LogException(exn)
                    fsFile

            ReparseResult(newFile, fsFile, 0)


module FSharpCodeCompletionContext =
    let private disableFullEvaluationKey = Key<obj>("FSharpCodeCompletionContext.disableFullEvaluationKey") 

    let isFullEvaluationDisabled (context: CodeCompletionContext) =
        let prevResult = context.Parameters.PreviousResult
        isNull prevResult || isNotNull (prevResult.GetData(disableFullEvaluationKey))

    let disableFullEvaluation (context: CodeCompletionContext) =
        context.PutData(disableFullEvaluationKey, disableFullEvaluationKey)

    let canBeIdentifierStart (token: ITreeNode) =
        let nodeType = getTokenType token
        if isNull nodeType then false else

        nodeType == FSharpTokenType.IDENTIFIER || nodeType == FSharpTokenType.UNDERSCORE || nodeType.IsKeyword

    let getFcsCompletionContext (fsFile: IFSharpFile) (document: IDocument) (offset: DocumentOffset) =
        let coords = offset.ToDocumentCoords()
        let lineText = document.GetLineText(coords.Line)
        let fcsPos = getPosFromCoords coords

        let fcsCompletionContext =
            fsFile.ParseTree |> Option.bind (fun parseTree ->
                ParsedInput.TryGetCompletionContext(fcsPos, parseTree, lineText))

        { PartialName = QuickParse.GetPartialLongNameEx(lineText, int coords.Column - 1)
          CompletionContext = fcsCompletionContext
          Coords = coords
          LineText = lineText
          DisplayContext = Unchecked.defaultof<_> }


type FSharpReparsedCodeCompletionContext(file: IFSharpFile, treeTextRange, newText) =
    inherit ReparsedCodeCompletionContext(file, treeTextRange, newText)

    member this.GetFcsContext() =
        let node = this.TreeNode
        let documentOffset = node.GetDocumentEndOffset()
        let file = node.GetContainingFile().As<IFSharpFile>()
        if isNull file then FcsCodeCompletionContext.Invalid else

        let document =
            match file.StandaloneDocument with
            | null -> file.GetSourceFile().Document
            | document -> document
            |> notNull

        FSharpCodeCompletionContext.getFcsCompletionContext file document documentOffset

    static member FixReferenceOwnerUnderTransaction(node: ITreeNode) =
        let psiServices = node.GetPsiServices()
        use cookie = PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, "FixReferenceOwner")
        FSharpReparsedCodeCompletionContext.FixReferenceOwner(node)

    static member FixReferenceOwner(node: ITreeNode) =
        match node with
        | TokenType FSharpTokenType.RESERVED_LITERAL_FORMATS token ->
            match token.Parent.As<IConstExpr>() with
            | null -> node
            | parent ->

            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()

            let text = token.GetText()
            let literalText = text.SubstringBeforeLast(".", StringComparison.Ordinal)

            let factory = token.CreateElementFactory()
            let constExpr = factory.CreateConstExpr(literalText)

            let refExpr = factory.CreateReferenceExpr($"_{text}")
            refExpr.SetQualifier(constExpr) |> ignore

            let refExpr = ModificationUtil.ReplaceChild(parent, refExpr)
            refExpr.Identifier :> _

        | _ -> node

    static member FixReferenceOwner(treeNode: ITreeNode, referenceRange: TreeTextRange) =
        let root = treeNode.Root().As<TreeElement>()
        if isNull root then () else

        let nodes = root.FindContainingNodesAt(referenceRange)
        let node = nodes.FirstOrDefault()
        FSharpReparsedCodeCompletionContext.FixReferenceOwner(node) |> ignore

    override this.GetReparseContext(file, range) =
        FSharpReparseContext(file :?> IFSharpFile, range) :>  _

    override this.FindReference(referenceRange, treeNode) =
        FSharpReparsedCodeCompletionContext.FixReferenceOwner(treeNode, referenceRange)
        treeNode.FindReferencesAt(referenceRange) |> Seq.tryHead |> Option.toObj

    override this.GetRangeOfReference _ =
        treeTextRange.ExtendRight(1)


type FSharpCodeCompletionContext(context: CodeCompletionContext, fcsCompletionContext: FcsCodeCompletionContext,
        reparsedContext: FSharpReparsedCodeCompletionContext, isQualified: bool, tokenBeforeCaret: ITreeNode,
        tokenAtCaret: ITreeNode, lookupRanges: TextLookupRanges, psiModule: IPsiModule, nodeInFile: ITreeNode,
        xmlDocService) =
    inherit ClrSpecificCodeCompletionContext(context, psiModule, nodeInFile)

    member val IsInAttributeContext =
        let reference = reparsedContext.Reference
        if isNull reference then false else

        match reference.GetTreeNode() with
        | :? ITypeReferenceName as name -> isNotNull (AttributeNavigator.GetByReferenceName(name))
        | _ -> false

    member this.EnableImportCompletion =
        let parameters = context.Parameters
        (parameters.Multiplier <> 1 || parameters.EvaluationMode = EvaluationMode.Full) &&

        not (FSharpCodeCompletionContext.isFullEvaluationDisabled context)

    member val ParseAndCheckResults =
        InterruptibleLazy<FSharpParseAndCheckResults option>(fun _ ->
            match context.File with
            | :? IFSharpFile as fsFile when fsFile.ParseResults.IsSome ->
                fsFile.GetParseAndCheckResults(true, "FSharpCodeCompletionContext.FcsCheckResults")
            | _ -> None
        )

    override this.ContextId = "FSharpCodeCompletionContext"

    member this.FcsCompletionContext = fcsCompletionContext
    member this.TokenBeforeCaret = tokenBeforeCaret
    member this.TokenAtCaret = tokenAtCaret
    member this.Ranges = lookupRanges
    member this.XmlDocService = xmlDocService
    member this.ReparsedContext = reparsedContext
    member this.IsQualified = isQualified

    member this.InsideToken =
        isNotNull tokenBeforeCaret && tokenBeforeCaret == tokenAtCaret

    member this.IsBasicOrSmartCompletion =
        let completionType = context.CodeCompletionType
        completionType = CodeCompletionType.SmartCompletion || completionType = CodeCompletionType.BasicCompletion

    member this.GetCheckResults() =
        this.ParseAndCheckResults.Value |> Option.map (fun results -> results.CheckResults)

[<IntellisensePart(Instantiation.DemandAnyThreadUnsafe)>]
type FSharpCodeCompletionContextProvider(fsXmlDocService: FSharpXmlDocService) =
    inherit CodeCompletionContextProviderBase()

    override this.IsApplicable(context) = context.File :? IFSharpFile

    override this.GetCompletionContext(context) =
        let fsFile = context.File.As<IFSharpFile>()
        if isNull fsFile then null else

        // todo: enable completion on selection to match C# behaviour
        let selectedTreeRange = context.SelectedTreeRange
        if selectedTreeRange.Length > 0 then null else

        // todo: fix for empty file via reparsed context
        let treeNode = fsFile.FindNodeAt(selectedTreeRange)
        if isNull treeNode then null else

        let document = context.Document
        let psiModule = fsFile.GetPsiModule()
        let caretOffset = context.CaretDocumentOffset

        let tokenAtCaret = fsFile.FindTokenAt(caretOffset)
        let tokenBeforeCaret = fsFile.FindTokenAt(caretOffset - 1)

        let isIdentifierStart = FSharpCodeCompletionContext.canBeIdentifierStart tokenBeforeCaret
        let completedRangeStart = if isIdentifierStart then tokenBeforeCaret.GetDocumentStartOffset() else caretOffset

        let reparsedContext =
            FSharpReparsedCodeCompletionContext(fsFile, selectedTreeRange, FSharpCompletionUtil.DummyIdentifier)
        reparsedContext.Init()

        let reference = reparsedContext.Reference.As<FSharpSymbolReference>()
        let isQualified = isNotNull reference && reference.IsQualified

        let lookupRanges =
            let completedRange = DocumentRange(&completedRangeStart, &caretOffset)
            let lookupRanges = CodeCompletionContextProviderBase.GetTextLookupRanges(context, completedRange)
            if not (FSharpCodeCompletionContext.canBeIdentifierStart tokenAtCaret) then lookupRanges else

            let tokenAtEndOffset = tokenAtCaret.GetDocumentEndOffset()
            lookupRanges.WithReplaceRange(DocumentRange(&completedRangeStart, &tokenAtEndOffset))

        let fcsContext = FSharpCodeCompletionContext.getFcsCompletionContext fsFile document caretOffset

        FSharpCodeCompletionContext(context, fcsContext, reparsedContext, isQualified, tokenBeforeCaret,
            tokenAtCaret, lookupRanges, psiModule, treeNode, fsXmlDocService) :> _
