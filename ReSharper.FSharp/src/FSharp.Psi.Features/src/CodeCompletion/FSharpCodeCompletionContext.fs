namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open System
open System.Linq
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Symbols
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
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
open JetBrains.Util
open JetBrains.Util.Extension
open JetBrains.Util.Logging

type FcsCodeCompletionContext =
    { PartialName: PartialLongName
      CompletionContext: CompletionContext option
      LineText: string
      Coords: DocumentCoords
      mutable DisplayContext: FSharpDisplayContext }


type FSharpReparseContext(fsFile: IFSharpFile, treeTextRange: TreeTextRange) =
    let [<Literal>] moniker = "F# reparse context"

    interface IReparseContext with
        member this.GetReparseResult(newText) =
            // todo: remove standalone document, try to make it incremental
            let source = fsFile.GetText().Insert(treeTextRange.StartOffset.Offset, newText)
            let documentFactory = Shell.Instance.GetComponent<IInMemoryDocumentFactory>()
            let document = documentFactory.CreateSimpleDocumentFromText(source, moniker)

            let parser = fsFile.GetFSharpLanguageService().CreateParser(document)
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
                    Logger.GetLogger<FSharpReparseContext>().LogException(exn)
                    fsFile

            ReparseResult(newFile, fsFile, 0)


type FSharpReparsedCodeCompletionContext(file: IFSharpFile, treeTextRange, newText) =
    inherit ReparsedCodeCompletionContext(file, treeTextRange, newText)

    static member FixReferenceOwner(node: ITreeNode) =
        match node with
        | TokenType FSharpTokenType.RESERVED_LITERAL_FORMATS token ->
            match token.Parent.As<IConstExpr>() with
            | null -> node
            | parent ->

            // todo: prevent the transaction in sandbox?
            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()
            use transactionCookie =
                PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(node.GetPsiServices(), "FixReferenceOwner")

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

        let node = root.FindContainingNodesAt(referenceRange).FirstOrDefault()
        FSharpReparsedCodeCompletionContext.FixReferenceOwner(node) |> ignore

    override this.GetReparseContext(file, range) =
        FSharpReparseContext(file :?> IFSharpFile, range) :>  _

    override this.FindReference(referenceRange, treeNode) =
        FSharpReparsedCodeCompletionContext.FixReferenceOwner(treeNode, referenceRange)
        treeNode.FindReferencesAt(referenceRange) |> Seq.tryHead |> Option.toObj

    override this.GetRangeOfReference _ =
        treeTextRange


type FSharpCodeCompletionContext(context: CodeCompletionContext, fcsCompletionContext: FcsCodeCompletionContext,
        reparsedContext: FSharpReparsedCodeCompletionContext, isQualified: bool, tokenBeforeCaret: ITreeNode,
        tokenAtCaret: ITreeNode, lookupRanges: TextLookupRanges, psiModule: IPsiModule, nodeInFile: ITreeNode,
        xmlDocService) =
    inherit ClrSpecificCodeCompletionContext(context, psiModule, nodeInFile)

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


[<IntellisensePart>]
type FSharpCodeCompletionContextProvider(fsXmlDocService: FSharpXmlDocService) =
    inherit CodeCompletionContextProviderBase()

    let canBeIdentifierStart (token: ITreeNode) =
        let nodeType = getTokenType token
        if isNull nodeType then false else

        nodeType == FSharpTokenType.IDENTIFIER || nodeType == FSharpTokenType.UNDERSCORE || nodeType.IsKeyword


    override this.IsApplicable(context) = context.File :? IFSharpFile

    override this.GetCompletionContext(context) =
        let fsFile = context.File.As<IFSharpFile>()
        if isNull fsFile then null else

        // todo: enable completion on selection to match C# behaviour
        let selectedTreeRange = context.SelectedTreeRange
        if selectedTreeRange.Length > 0 then null else

        let treeNode = fsFile.FindNodeAt(selectedTreeRange)
        if isNull treeNode then null else

        let document = context.Document
        let psiModule = fsFile.GetPsiModule()
        let caretOffset = context.CaretDocumentOffset

        let tokenAtCaret = fsFile.FindTokenAt(caretOffset)
        let tokenBeforeCaret = fsFile.FindTokenAt(caretOffset - 1)

        let isIdentifierStart = canBeIdentifierStart tokenBeforeCaret
        let completedRangeStart = if isIdentifierStart then tokenBeforeCaret.GetDocumentStartOffset() else caretOffset

        let reparsedContext =
            FSharpReparsedCodeCompletionContext(fsFile, selectedTreeRange, FSharpCompletionUtil.DummyIdentifier)
        reparsedContext.Init()

        let reference = reparsedContext.Reference.As<FSharpSymbolReference>()
        let isQualified = isNotNull reference && reference.IsQualified

        let lookupRanges =
            let completedRange = DocumentRange(&completedRangeStart, &caretOffset)
            let lookupRanges = CodeCompletionContextProviderBase.GetTextLookupRanges(context, completedRange)
            if not (canBeIdentifierStart tokenAtCaret) then lookupRanges else

            let tokenAtEndOffset = tokenAtCaret.GetDocumentEndOffset()
            lookupRanges.WithReplaceRange(DocumentRange(&completedRangeStart, &tokenAtEndOffset))

        let fcsContext =
            let coords = document.GetCoordsByOffset(caretOffset.Offset)
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

        FSharpCodeCompletionContext(context, fcsContext, reparsedContext, isQualified, tokenBeforeCaret,
            tokenAtCaret, lookupRanges, psiModule, treeNode, fsXmlDocService) :> _
