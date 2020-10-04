[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.FSharpErrorUtil

open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

let getTreeNodesDocumentRange (startNode: ITreeNode) (endNode: ITreeNode) =
    let startOffset = startNode.GetDocumentStartOffset()
    let endOffset = endNode.GetDocumentEndOffset()
    DocumentRange(&startOffset, &endOffset)

let getUpcastRange (upcastExpr: IUpcastExpr) =
    if isValid upcastExpr && isValid upcastExpr.OperatorToken && isValid upcastExpr.TypeUsage then
        getTreeNodesDocumentRange upcastExpr.OperatorToken upcastExpr.TypeUsage
    else
        DocumentRange.InvalidRange

let getIndexerArgListRange (indexerExpr: IItemIndexerExpr) =
    match indexerExpr.IndexerArgList with
    | null -> indexerExpr.GetHighlightingRange()
    | argList -> argList.GetHighlightingRange()

let getLetTokenText (token: ITokenNode) =
    let tokenType = getTokenType token
    let tokenType = if isNull tokenType then FSharpTokenType.LET else tokenType 
    tokenType.TokenRepresentation

let getExpressionsRanges (exprs: IFSharpExpression seq) =
    exprs |> Seq.map (fun x -> if isValid x then x.GetHighlightingRange() else DocumentRange.InvalidRange) 

let getRefExprNameRange (refExpr: IReferenceExpr) =
    match refExpr.Identifier with
    | null -> refExpr.GetHighlightingRange()
    | identifier -> identifier.GetHighlightingRange()

let rec getUnusedExpr (expr: IFSharpExpression) =
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
    if isNull referenceName || not (referenceName.ShortName |> endsWith "Attribute") then
        DocumentRange.InvalidRange else

    referenceName.GetDocumentEndOffset().ExtendLeft("Attribute".Length)

let getQualifierRange (element: ITreeNode) =
    match element with
    | :? IReferenceExpr as refExpr -> getTreeNodesDocumentRange refExpr.Qualifier refExpr.Delimiter
    | :? IReferenceName as referenceName -> getTreeNodesDocumentRange referenceName.Qualifier referenceName.Delimiter

    | :? ITypeExtensionDeclaration as typeExtension ->
        getTreeNodesDocumentRange typeExtension.QualifierReferenceName typeExtension.Delimiter

    | _ -> DocumentRange.InvalidRange

/// Assuming `|>` or `<|` were resolved beforehand. 
let getFunctionApplicationRange (appExpr: IAppExpr) =
    match appExpr, appExpr.FunctionExpression with
    | :? IPrefixAppExpr, funExpr -> funExpr.GetHighlightingRange()
    | :? IBinaryAppExpr as binaryApp, (:? IReferenceExpr as refExpr) ->
        match refExpr.ShortName with
        | "|>" -> getTreeNodesDocumentRange refExpr binaryApp.RightArgument
        | "<|" -> getTreeNodesDocumentRange binaryApp.LeftArgument refExpr
        | _ -> DocumentRange.InvalidRange
    | _ -> DocumentRange.InvalidRange

let getFunctionExpr (appExpr: IAppExpr) =
    match appExpr, appExpr.FunctionExpression with
    | :? IPrefixAppExpr, funExpr -> funExpr
    | :? IBinaryAppExpr as binaryApp, (:? IReferenceExpr as refExpr) ->
        match refExpr.ShortName with
        | "|>" -> binaryApp.RightArgument
        | "<|" -> binaryApp.LeftArgument
        | _ -> null
    | _ -> null

let getReferenceExprName (expr: IFSharpExpression) =
    match expr.IgnoreInnerParens() with
    | :? IReferenceExpr as refExpr -> refExpr.ShortName
    | _ -> SharedImplUtil.MISSING_DECLARATION_NAME

let rec isSimpleQualifiedName (expr: IReferenceExpr) =
    if isNotNull expr.TypeArgumentList then false else
    match expr.Qualifier with
    | :? IReferenceExpr as expr -> isSimpleQualifiedName expr
    | null -> true
    | _ -> false

let getLambdaCanBeReplacedWarningText (replaceCandidate: IFSharpExpression) =
    match replaceCandidate with
    | :? IReferenceExpr as x when isSimpleQualifiedName x ->
        sprintf "Lambda can be replaced with '%s'" x.QualifiedName
    | _ -> "Lambda can be simplified"

let getExpressionCanBeReplacedWithIdWarningText (expr: IFSharpExpression) =
    match expr with
    | :? ILambdaExpr as lambda ->
        if lambda.PatternsEnumerable.CountIs(1) then "Lambda can be replaced with 'id'"
        else "Lambda body can be replaced with 'id'"
    | _ -> "Expression can be replaced with 'id'"
