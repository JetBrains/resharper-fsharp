[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.FSharpErrorUtil

open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

let getUpcastRange (upcastExpr: IUpcastExpr) =
    if not (isValid upcastExpr && isValid upcastExpr.OperatorToken) then DocumentRange.InvalidRange else

    let documentRange = upcastExpr.GetNavigationRange()
    let operatorRange = upcastExpr.OperatorToken.GetNavigationRange()

    DocumentRange(documentRange.Document, TextRange(operatorRange.StartOffset.Offset, documentRange.EndOffset.Offset))

let getIndexerArgListRange (indexerExpr: IItemIndexerExpr) =
    match indexerExpr.IndexerArgList with
    | null -> indexerExpr.GetHighlightingRange()
    | argList -> argList.GetHighlightingRange()

let getLetTokenText (token: ITokenNode) =
    let tokenType = getTokenType token
    let tokenType = if isNull tokenType then FSharpTokenType.LET else tokenType 
    tokenType.TokenRepresentation

let getExpressionsRanges (exprs: ISynExpr seq) =
    exprs |> Seq.map (fun x -> if isValid x then x.GetHighlightingRange() else DocumentRange.InvalidRange) 

let getRefExprNameRange (refExpr: IReferenceExpr) =
    match refExpr.Identifier with
    | null -> refExpr.GetHighlightingRange()
    | identifier -> identifier.GetHighlightingRange()

let rec getUnusedExpr (expr: ISynExpr) =
    match expr with
    | :? ILetOrUseExpr as letExpr ->
        let inExpr = letExpr.InExpression
        if isNotNull inExpr then getUnusedExpr inExpr else expr

    | :? ISequentialExpr as seqExpr ->
        let lastExpr = seqExpr.Expressions.LastOrDefault()
        if isNotNull lastExpr then getUnusedExpr lastExpr else expr

    | _ -> expr

let getAttributeSuffixRange (attribute: IAttribute) =
    let referenceName = attribute.ReferenceName
    if isNull referenceName || not (referenceName.ShortName |> endsWith "Attribute") then DocumentRange.InvalidRange else

    referenceName.GetDocumentEndOffset().ExtendLeft("Attribute".Length)
