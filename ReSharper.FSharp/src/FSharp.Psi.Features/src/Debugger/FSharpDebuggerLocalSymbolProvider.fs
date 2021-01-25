namespace JetBrains.ReSharper.Plugins.FSharp.Services.Debugger

open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.Range
open FSharp.Compiler.SourceCodeServices
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Debugger
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

[<Language(typeof<FSharpLanguage>)>]
type FSharpDebuggerLocalSymbolProvider() =
    interface IDebuggerLocalSymbolProvider with
        member x.FindLocalDeclarationAt(file: IFile, range: DocumentRange, name: string) =
            match file.AsFSharpFile() with
            | null -> null, null
            | fsFile ->

            match fsFile.ParseTree with
            | None -> null, null
            | Some parseTree ->

            let pos = getPosFromDocumentOffset range.EndOffset
            let mutable declRange = None

            let visitor =
                let inline (|SuitableIdent|_|) (ident: Ident) = 
                    if ident.idText = name && (posLt ident.idRange.End pos || posEq ident.idRange.End pos) then
                        Some ident.idRange
                    else None

                let updateDeclRange (range: range) =
                    match declRange with
                    | None -> 
                        declRange <- Some range
                    | Some oldDeclRange when posGt range.Start oldDeclRange.Start ->
                        declRange <- Some range
                    | _ -> ()

                let visitPat pat defaultTraverse =
                    match pat with
                    | SynPat.Named(_, SuitableIdent range, false, _, _) -> updateDeclRange range 
                    | _ -> ()
                    defaultTraverse pat

                { new AstTraversal.AstVisitorBase<_>() with
                    member this.VisitExpr(_, traverseSynExpr, defaultTraverse, expr) = 
                        match expr with
                        | SynExpr.For(ident = SuitableIdent range) -> updateDeclRange range 
                        | SynExpr.ForEach(pat = pat) -> visitPat pat ignore
                        | SynExpr.App(argExpr = SynExpr.Ident (SuitableIdent range)) -> updateDeclRange range
                        | _ -> ()
                        defaultTraverse expr

                    member this.VisitPat(defaultTraverse, pat) = visitPat pat defaultTraverse 

                    member this.VisitLetOrUse(_, _, bindings, range) =
                        bindings |> List.tryPick (fun (Binding(headPat = headPat)) ->
                            visitPat headPat (fun _ -> None))

                    member this.VisitMatchClause(defaultTraverse, (Clause(pat, _, _, _, _) as clause)) = 
                        visitPat pat (fun _ -> None) |> Option.orElseWith (fun _ -> defaultTraverse clause) }

            AstTraversal.Traverse(pos, parseTree, visitor) |> ignore

            match declRange with
            | Some declRange ->
                let endOffset = getTreeEndOffset range.Document declRange
                let treeNode = fsFile.FindTokenAt(endOffset - 1)
                match treeNode.GetContainingNode<IFSharpDeclaration>() with
                | null -> treeNode, null
                | declaration -> treeNode, declaration.DeclaredElement

            | None -> null, null

        member x.FindContainingFunctionDeclarationBody(treeNode) =
            treeNode // todo: refactor to get tree node instead of function declaration in SDK
