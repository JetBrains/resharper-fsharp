[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.FSharpErrorUtil

open FSharp.Compiler.Symbols
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.PsiUtil
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree

type FSharpDiagnosticTypeInfo =
    { IsTupleType: bool; TypeString: string } with

    static member Create(fcsType: FSharpType, displayContext: FSharpDisplayContext) =
        { IsTupleType = fcsType.IsTupleType
          TypeString = fcsType.Format(displayContext) }

let getDocumentRange (node: #ITreeNode) =
    node.GetHighlightingRange()

let secondaryRangesFromNode (node: #ITreeNode) =
    match node with
    | null -> Seq.empty<DocumentRange>
    | node -> [| node.GetHighlightingRange() |] :> _

let getTreeNodesDocumentRange (startNode: ITreeNode) (endNode: ITreeNode) =
    let startOffset = startNode.GetDocumentStartOffset()
    let endOffset = endNode.GetDocumentEndOffset()
    DocumentRange(&startOffset, &endOffset)

let getUpcastRange (upcastExpr: IUpcastExpr) =
    if isValid upcastExpr && isValid upcastExpr.OperatorToken && isValid upcastExpr.TypeUsage then
        getTreeNodesDocumentRange upcastExpr.OperatorToken upcastExpr.TypeUsage
    else
        DocumentRange.InvalidRange

let getAsPatternRange (asPat: IAsPat) =
    if isValid asPat && isValid asPat.LeftPattern && isValid asPat.AsKeyword then
        getTreeNodesDocumentRange asPat.LeftPattern asPat.AsKeyword
    else
        DocumentRange.InvalidRange

let getIndexerDotRange (indexerExpr: IItemIndexerExpr) =
    if not (isValid indexerExpr) then DocumentRange.InvalidRange else 

    let delimiter = indexerExpr.Delimiter
    if not (isValid delimiter) then DocumentRange.InvalidRange else

    delimiter.GetDocumentRange()

let getLetTokenText (token: ITokenNode) =
    let tokenType = getTokenType token
    let tokenType = if isNull tokenType then FSharpTokenType.LET else tokenType
    tokenType.TokenRepresentation

let getNodeRanges (exprs: #ITreeNode seq) =
    exprs |> Seq.map (fun x -> x.GetHighlightingRange())

let getRefExprNameRange (refExpr: IReferenceExpr) =
    match refExpr.Identifier with
    | null -> refExpr.GetHighlightingRange()
    | identifier -> identifier.GetHighlightingRange()

let rec getResultExpr (expr: IFSharpExpression) =
    match expr with
    | :? ILetOrUseExpr as letExpr ->
        let inExpr = letExpr.InExpression
        if isNotNull inExpr then getResultExpr inExpr else expr

    | :? ISequentialExpr as seqExpr ->
        let lastExpr = seqExpr.Expressions.LastOrDefault()
        if isNotNull lastExpr then getResultExpr lastExpr else expr

    | :? IParenOrBeginEndExpr as parenExpr ->
        let innerExpr = parenExpr.InnerExpression
        if isNotNull innerExpr && not parenExpr.IsSingleLine then getResultExpr innerExpr else expr

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

let getExprPresentableName (expr: IFSharpExpression) =
    match expr.IgnoreInnerParens() with
    | :? IReferenceExpr as refExpr -> $"'{refExpr.ShortName}'"
    | :? ILambdaExpr -> "lambda"
    | :? ITypeTestExpr -> "type test"
    | :? ICastExpr -> "type cast"
    | _ -> SharedImplUtil.MISSING_DECLARATION_NAME

let rec isSimpleQualifiedName (expr: IReferenceExpr) =
    isNull expr.TypeArgumentList &&
    isNull (DotLambdaExprNavigator.GetByExpression(expr)) &&

    match expr.Qualifier with
    | :? IReferenceExpr as expr -> isSimpleQualifiedName expr
    | null -> true
    | _ -> false

let getLambdaCanBeReplacedWarningText (replaceCandidate: IFSharpExpression) =
    match replaceCandidate with
    | :? IReferenceExpr as x when isSimpleQualifiedName x ->
        sprintf "Lambda can be replaced with '%s'" x.QualifiedName
    | _ -> "Lambda can be simplified"

let getInterfaceImplHeaderRange (interfaceImpl: IInterfaceImplementation) =
    match interfaceImpl.NameIdentifier with
    | null -> interfaceImpl.GetDocumentRange()
    | identifier -> identifier.GetDocumentRange()

let getSecondBindingKeyword (bindings: ILetBindings) =
    let bindings = bindings.Bindings
    if Seq.isEmpty bindings then DocumentRange.InvalidRange else

    bindings
    |> Seq.tail
    |> Seq.tryHead
    |> Option.bind (fun b -> Option.ofObj b.BindingKeyword)
    |> Option.map getDocumentRange
    |> Option.defaultValue DocumentRange.InvalidRange

let getParameterOwnerPatParametersRange (pat : IParametersOwnerPat) =
    let parameters = pat.Parameters
    if parameters.IsEmpty then pat.GetHighlightingRange() else

    let first = parameters.First()
    let last = parameters.Last()

    getTreeNodesDocumentRange first last

let getMatchLikeExprIncompleteRange (expr: IMatchLikeExpr) =
    let treeNode: ITreeNode = 
        match expr with
        | :? IMatchExpr as matchExpr -> matchExpr.Expression
        | :? IMatchLambdaExpr as functionExpr -> functionExpr.FunctionKeyword
        | _ -> expr

    treeNode.GetHighlightingRange()

let getNestedRecordUpdateRange (outer: IRecordFieldBinding) (inner: IRecordFieldBinding) =
    let recordExpr = RecordExprNavigator.GetByFieldBinding(inner)
    getTreeNodesDocumentRange outer.EqualsToken recordExpr.WithKeyword
