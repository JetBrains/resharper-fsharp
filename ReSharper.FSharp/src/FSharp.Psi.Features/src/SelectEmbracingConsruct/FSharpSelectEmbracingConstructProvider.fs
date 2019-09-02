namespace JetBrains.ReSharper.Plugins.FSharp.Services.SelectEmbracingConstruct

open System
open FSharp.Compiler
open FSharp.Compiler.Ast
open FSharp.Compiler.SourceCodeServices.AstTraversal
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.SelectEmbracingConstruct
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

[<AllowNullLiteral>]
type FSharpSelection(file, range, parentRanges: DocumentRange list) =
    inherit SelectedRangeBase<IFSharpFile>(file, range)

    override x.Parent =
        match parentRanges with
        | parent :: rest -> FSharpSelection(file, parent, rest) :> _
        | _ -> null

    override x.ExtendToWholeLine = ExtendToTheWholeLinePolicy.DO_NOT_EXTEND


type FSharpTokenPartSelection(fsFile, range, token, parentRanges: DocumentRange list) =
    inherit TokenPartSelection<IFSharpFile>(fsFile, range, token)

    let tokenText = token.GetText()

    let trim left right =
        let range = token.GetTreeTextRange()
        if range.Length >= left + right then
            tokenText.Substring(left, tokenText.Length - left - right), left
        else tokenText, left

    let getParentSelection () =
        match parentRanges with
        | parent :: rest -> FSharpSelection(fsFile, parent, rest)
        | _ -> null
    
    override x.Parent =
        let text, start =
            let tokenType = token.GetTokenType()
            if tokenType.IsStringLiteral then
                match tokenType.GetLiteralType() with
                | FSharpLiteralType.Character
                | FSharpLiteralType.RegularString -> trim 1 1
                | FSharpLiteralType.VerbatimString -> trim 2 1
                | FSharpLiteralType.TripleQuoteString -> trim 3 3
                | FSharpLiteralType.ByteArray -> trim 1 2

            elif tokenType == FSharpTokenType.IDENTIFIER then
                let tokenText = token.GetText()
                if tokenText.Length > 4 &&
                        tokenText.StartsWith("``", StringComparison.Ordinal) &&
                        tokenText.EndsWith("``", StringComparison.Ordinal) then
                    trim 2 2
                else tokenText, 0

            elif tokenType == FSharpTokenType.LINE_COMMENT then
                let left = if tokenText.StartsWith("///") then 3 else 2
                trim left 0

            elif tokenType == FSharpTokenType.BLOCK_COMMENT then
                let right = if tokenText.EndsWith("*)") then 2 else 0
                trim 2 right

            else tokenText, 0

        if range.IsValid() then
            let localRange = range.Shift(-token.GetTreeStartOffset().Offset - start)
            let range = TokenPartSelection<_>.GetLocalParent(text, localRange)
            if range.IsValid() && range.Contains(&localRange) then
                let range = range.Shift(token.GetTreeStartOffset() + start)
                FSharpTokenPartSelection(fsFile, range, token, parentRanges) :> _
            else
                getParentSelection () :> _
        else
            getParentSelection () :> _


type FSharpDotSelection(fsFile, range: TreeTextRange, parentRanges: DocumentRange list) =
    inherit DotSelection<IFSharpFile>(fsFile, range.StartOffset, range.Length = 0, false)

    override x.CreateTreeNodeSelection(tokenNode) =
        FSharpSelection(fsFile, tokenNode.GetDocumentRange(), parentRanges) :> _

    override x.IsWordToken(token) =
        let tokenType = token.GetTokenType()
        tokenType.IsIdentifier || tokenType.IsKeyword

    override x.IsLiteralToken(token) =
        let tokenType = token.GetTokenType()
        tokenType.IsConstantLiteral || tokenType.IsStringLiteral

    override x.IsSpaceToken(token) = token.GetTokenType().IsWhitespace
    override x.IsNewLineToken(token) = token.GetTokenType() = FSharpTokenType.NEW_LINE

    override x.GetParentInternal(token) =
        let tokenType = token.GetTokenType()
        if tokenType.IsComment || tokenType.IsStringLiteral || tokenType.IsIdentifier then
            FSharpTokenPartSelection(fsFile, TreeTextRange(range.StartOffset), token, parentRanges) :> _
        else null

    override x.CreateTokenPartSelection(node, range) =
        FSharpTokenPartSelection(fsFile, range, node, parentRanges) :> _

    override x.ExtendToWholeLine = ExtendToTheWholeLinePolicy.DO_NOT_EXTEND

[<ProjectFileType(typeof<FSharpProjectFileType>)>]
type FSharpSelectEmbracingConstructProvider() =
    let getRanges = function
        | TraverseStep.Expr(expr) -> [expr.Range]
        | TraverseStep.TypeDefn(typeDefn) -> [typeDefn.Range]
        | TraverseStep.MemberDefn(memberDefn) -> [memberDefn.Range]
        | _ -> []

    let mapLid (lid: LongIdent) =
        match lid with
        | [] -> []
        | id :: ids ->
            ids
            |> List.fold (fun acc id -> (id :: (List.head acc)) :: acc) [[id]]
            |> List.map (fun lid ->
                let lid = List.rev lid
                let range = Range.unionRanges (List.head lid).idRange (List.last lid).idRange
                TraverseStep.Expr (SynExpr.LongIdent (false, LongIdentWithDots(lid, []), None, range)))

    interface ISelectEmbracingConstructProvider with
        member x.IsAvailable _ = true

        member x.GetSelectedRange(sourceFile, documentRange) =
            match sourceFile.GetFSharpFile() with
            | null -> null
            | fsFile ->

            match fsFile.ParseTree with
            | None -> null
            | Some parseTree ->

            let mutable documentRange = documentRange
            let document = documentRange.Document
            let pos = getPosFromDocumentOffset documentRange.StartOffset
            let visitor = { new AstVisitorBase<_>() with
                member x.VisitExpr(path, _, defaultTraverse, expr) =
                    match expr with
                    | SynExpr.Ident _ | SynExpr.Const _ -> Some path
                    | SynExpr.LongIdent (_, lid, _, _) -> Some (mapLid lid.Lid @ path)
                    | _ -> defaultTraverse expr

                override this.VisitModuleDecl(defaultTraverse, decl) =
                    match decl with
                    | SynModuleDecl.Open(lid, _) -> Some(mapLid lid.Lid)
                    | _ -> defaultTraverse decl }

            let containingDeclarations =
                match fsFile.FindTokenAt(documentRange.StartOffset) with
                | null -> []
                | token ->
                    let rec getParents = function
                        | null -> []
                        | (t: ITreeNode) -> t.GetDocumentRange() :: getParents t.Parent
                    getParents token

            let ranges =
                match Traverse(pos, parseTree, visitor) with
                | Some traversePath ->
                    List.map getRanges traversePath
                    |> List.concat
                    |> List.map (getDocumentRange document)
                | None -> []
                |> List.append containingDeclarations
                |> List.filter (fun r -> r.Contains(&documentRange))
                |> List.sortBy (fun r -> r.Length)

            FSharpDotSelection(fsFile, fsFile.Translate(documentRange), ranges) :> _
