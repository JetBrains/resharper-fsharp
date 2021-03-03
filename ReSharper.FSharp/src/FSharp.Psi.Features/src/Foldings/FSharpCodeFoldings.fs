namespace JetBrains.ReSharper.Plugins.FSharp.Services.Foldings

open FSharp.Compiler.EditorServices.Structure
open JetBrains.DocumentModel
open JetBrains.ReSharper.Daemon.CodeFolding
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.TextControl.DocumentMarkup
open JetBrains.Util

module FSharpCodeFoldingAttributes =
    let [<Literal>] HashDirective = "ReSharper F# Hash Directives Block Folding"


[<RegisterHighlighter(FSharpCodeFoldingAttributes.HashDirective, EffectType = EffectType.FOLDING)>]
type FSharpCodeFoldingAttributes() = class end


type FSharpCodeFoldingProcess(logger: ILogger) =
    inherit TreeNodeVisitor<FoldingHighlightingConsumer>()
    let mutable processingFinished = false

    let getFoldingAttrId = function
        | Scope.Open -> CodeFoldingAttributes.IMPORTS_FOLDING_ATTRIBUTE
        | Scope.Attribute -> CodeFoldingAttributes.ATTRIBUTES_FOLDING_ATTRIBUTE
        | Scope.Comment -> CodeFoldingAttributes.COMMENTS_FOLDING_ATTRIBUTE
        | Scope.XmlDocComment -> CodeFoldingAttributes.DOCUMENTATION_COMMENTS_FOLDING_ATTRIBUTE
        | Scope.Member -> CodeFoldingAttributes.METHOD_FOLDING_ATTRIBUTE
        | Scope.HashDirective -> FSharpCodeFoldingAttributes.HashDirective
        | _ -> CodeFoldingAttributes.DEFAULT_FOLDING_ATTRIBUTE

    override x.VisitNode(element, context) =
        match element.GetContainingNode<IFSharpFile>() with
        | null -> ()
        | fsFile ->

        match fsFile.ParseTree with
        | None -> ()
        | Some parseTree ->

        let sourceFile = element.GetSourceFile()
        let document = sourceFile.Document
        let lines = [| for line in 0 .. (int (document.GetLineCount().Minus1())) do
                        yield document.GetLineText(docLine line) |]

        getOutliningRanges lines parseTree
        |> Seq.distinctBy (fun x -> x.Range.StartLine)
        |> Seq.iter (fun x ->
            let textRange = getTextRange document x.CollapseRange
            if textRange.IsEmpty then logger.Warn(sprintf "Empty folding: %O %A" textRange x) else

            let placeholder =
                match x.Scope with
                | Scope.Open -> "..."
                | _ ->

                let line = (docLine x.CollapseRange.StartLine).Minus1()
                let lineStart = document.GetLineStartOffset(line)
                let lineEnd = document.GetLineEndOffsetNoLineBreak(line)
                match TextRange(lineStart, lineEnd).Intersect(&textRange) with
                | range when not range.IsEmpty -> document.GetText(range) + " ..."
                | _ -> " ..."

            let highlightingId = getFoldingAttrId x.Scope
            let documentRange = DocumentRange(document, textRange)
            context.AddDefaultPriorityFolding(highlightingId, documentRange, placeholder))
                

    interface ICodeFoldingProcessor with
        member x.InteriorShouldBeProcessed(_,_) = false
        member x.IsProcessingFinished _ = processingFinished
        member x.ProcessAfterInterior(_,_) = ()
        member x.ProcessBeforeInterior(element, context) =
            processingFinished <- true
            match element with
            | :? IFSharpTreeNode as treeNode -> treeNode.Accept(x, context)
            | _ -> ()


[<Language(typeof<FSharpLanguage>)>]
type FSharpCodeFoldingProcessFactory(logger: ILogger) =
    interface ICodeFoldingProcessorFactory with
        member x.CreateProcessor() =
            FSharpCodeFoldingProcess(logger) :> _
