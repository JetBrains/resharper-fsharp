[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Util.PsiUtil

open FSharp.Compiler.Range
open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.TextControl
open JetBrains.Util.Text

type IFile with
    member x.AsFSharpFile() =
        match x with
        | :? IFSharpFile as fsFile -> fsFile
        | _ -> null

type IPsiSourceFile with
    member x.GetFSharpFile() =
        if isNull x then null else
        x.GetPrimaryPsiFile().AsFSharpFile()

type ITextControl with
    member x.GetFSharpFile(solution) =
        x.Document.GetPsiSourceFile(solution).GetFSharpFile()

type IFSharpFile with
    member x.ParseTree =
        match x.ParseResults with
        | Some parseResults -> parseResults.ParseTree
        | _ -> None

    member x.GetNode<'T when 'T :> ITreeNode and 'T : null>(document, range) =
        let offset = getStartOffset document range
        x.GetNode<'T>(DocumentOffset(document, offset))

    member x.GetNode<'T when 'T :> ITreeNode and 'T : null>(range: range) =
        let document = x.GetSourceFile().Document
        x.GetNode<'T>(document, range)

    member x.GetNode<'T when 'T :> ITreeNode and 'T : null>(documentOffset: DocumentOffset) =
        match x.FindTokenAt(documentOffset) with
        | null -> null
        | token -> token.GetContainingNode<'T>(true)

    member x.GetNode<'T when 'T :> ITreeNode and 'T : null>(documentRange: DocumentRange) =
        x.GetNode<'T>(documentRange.StartOffset)

type IFSharpTreeNode with
    member x.FSharpLanguageService =
        x.Language.LanguageService().As<IFSharpLanguageService>()

    member x.CreateElementFactory() =
        x.FSharpLanguageService.CreateElementFactory(x.GetPsiModule())

    member x.GetLineEnding() =
        let fsFile = x.FSharpFile
        fsFile.DetectLineEnding(fsFile.GetPsiServices()).GetPresentation()

type FSharpLanguage with
    member x.FSharpLanguageService =
        x.LanguageService().As<IFSharpLanguageService>()        


type ITreeNode with
        member x.IsChildOf(node: ITreeNode) =
            if isNull node then false else node.Contains(x)

        member x.GetIndent(document: IDocument) =
            let startOffset = x.GetDocumentStartOffset().Offset
            let startCoords = document.GetCoordsByOffset(startOffset)
            startOffset - document.GetLineStartOffset(startCoords.Line)

        member x.Indent =
            let document = x.GetSourceFile().Document
            x.GetIndent(document)

        member x.GetStartLine(document: IDocument) =
            document.GetCoordsByOffset(x.GetDocumentStartOffset().Offset).Line

        member x.GetEndLine(document: IDocument) =
            document.GetCoordsByOffset(x.GetDocumentEndOffset().Offset).Line
        
        member x.StartLine = x.GetStartLine(x.GetSourceFile().Document)
        member x.EndLine = x.GetEndLine(x.GetSourceFile().Document)

        member x.IsSingleLine =
            let document = x.GetSourceFile().Document
            x.GetStartLine(document) = x.GetEndLine(document)

let getNode<'T when 'T :> ITreeNode and 'T : null> (fsFile: IFSharpFile) (range: DocumentRange) =
    let node = fsFile.GetNode<'T>(range)
    if isNull node then failwithf "Couldn't get %O from range %O" typeof<'T>.Name range else
    node

let (|TokenType|_|) tokenType (treeNode: ITreeNode) =
    if isNotNull treeNode && treeNode.GetTokenType() == tokenType then Some treeNode else None

let (|Whitespace|_|) (treeNode: ITreeNode) =
    if isNotNull treeNode && treeNode.GetTokenType() == FSharpTokenType.WHITESPACE then Some treeNode else None

let (|IgnoreParenPat|) (pat: ISynPat) = pat.IgnoreParentParens()

let (|IgnoreInnerParenExpr|) (expr: ISynExpr) =
    match expr with
    | :? IParenExpr as parenExpr -> parenExpr.InnerExpression
    | _ -> expr

[<AutoOpen>]
module PsiModificationUtil =
    let replace oldChild newChild =
        ModificationUtil.ReplaceChild(oldChild, newChild) |> ignore

    let replaceWithCopy oldChild newChild =
        replace oldChild (newChild.Copy())

    let replaceWithToken oldChild (newChildTokenType: TokenNodeType) =
        replace oldChild (newChildTokenType.CreateLeafElement())

    let deleteChildRange first last =
        ModificationUtil.DeleteChildRange(first, last)


let getPrevNodeOfType nodeType (node: ITreeNode) =
    let mutable prev = node.PrevSibling
    while prev.NodeType != nodeType do
        prev <- prev.PrevSibling
    prev

let getNextNodeOfType nodeType (node: ITreeNode) =
    let mutable next = node.NextSibling
    while next.NodeType != nodeType do
        next <- next.NextSibling
    next


let rec getNonPatParent (pat: ISynPat) =
    match pat.Parent with
    | :? ISynPat as pat -> getNonPatParent pat
    | node -> node


let isValid (node: ITreeNode) =
    isNotNull node && node.IsValid()
