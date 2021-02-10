namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open FSharp.Compiler
open FSharp.Compiler.SourceCodeServices
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree


type FcsCodeCompletionContext =
    { PartialName: PartialLongName
      CompletionContext: CompletionContext option
      LineText: string
      Coords: DocumentCoords
      mutable DisplayContext: FSharpDisplayContext }


type FSharpCodeCompletionContext(context: CodeCompletionContext, fcsCompletionContext: FcsCodeCompletionContext,
        tokenBeforeCaret: ITreeNode, tokenAtCaret: ITreeNode, lookupRanges: TextLookupRanges,
        psiModule: IPsiModule, nodeInFile: ITreeNode, xmlDocService) =
    inherit ClrSpecificCodeCompletionContext(context, psiModule, nodeInFile)

    override this.ContextId = "FSharpCodeCompletionContext"

    member this.FcsCompletionContext = fcsCompletionContext
    member this.TokenBeforeCaret = tokenBeforeCaret
    member this.TokenAtCaret = tokenAtCaret
    member this.Ranges = lookupRanges
    member this.XmlDocService = xmlDocService

    member this.InsideToken =
        isNotNull tokenBeforeCaret && tokenBeforeCaret == tokenAtCaret


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
                    UntypedParseImpl.TryGetCompletionContext(fcsPos, parseTree, lineText))

            { PartialName = QuickParse.GetPartialLongNameEx(lineText, (int) coords.Column - 1)
              CompletionContext = fcsCompletionContext
              Coords = coords
              LineText = lineText
              DisplayContext = Unchecked.defaultof<_> }

        FSharpCodeCompletionContext(
            context, fcsContext, tokenBeforeCaret, tokenAtCaret, lookupRanges, psiModule, treeNode,
            fsXmlDocService) :> _
