namespace JetBrains.ReSharper.Plugins.FSharp.Services.Debugger

open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Debugger
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open Microsoft.FSharp.Compiler
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Ast

[<Language(typeof<FSharpLanguage>)>]
type FSharpDebuggerLocalSymbolProvider() =
    interface IDebuggerLocalSymbolProvider with
        member __.FindLocalDeclarationAt(file: IFile, range: DocumentRange, name: string): ITreeNode * IDeclaredElement =
            match file with
            | :? IFSharpFile as fsFile ->
                match fsFile.ParseResults with
                | Some parseResults ->
                    match parseResults.ParseTree with
                    | Some parseTree ->
                        let pos = range.Document.GetPos(range.EndOffset.Offset)
                        let mutable declRange = None
                        
                        let visitor =
                            let visitPat pat defaultTraverse =
                                match pat with
                                | SynPat.Named(_, id, false, _, range) when id.idText = name &&
                                                                            (Range.posLt range.End pos || Range.posEq range.End pos) -> 
                                    match declRange with
                                    | None -> 
                                        declRange <- Some range
                                    | Some oldDeclRange when Range.posGt range.Start oldDeclRange.Start ->
                                        declRange <- Some range
                                    | _ -> ()
                                | _ -> ()
                                
                                defaultTraverse pat
                                
                            { new AstTraversal.AstVisitorBase<_>() with
                                member this.VisitExpr(_, traverseSynExpr, defaultTraverse, expr) = defaultTraverse expr

                                member this.VisitPat(defaultTraverse, pat) = visitPat pat defaultTraverse 
                                
                                member this.VisitLetOrUse(bindings, range) =
                                    bindings |> List.tryPick (fun (Binding(headPat = headPat)) ->
                                        visitPat headPat (fun _ -> None)
                                    )
                            }
                        
                        AstTraversal.Traverse(pos, parseTree, visitor) |> ignore
                        match declRange with
                        | Some declRange ->
                            let endOffset = range.Document.GetTreeEndOffset(declRange)
                            let treeNode = fsFile.FindTokenAt(endOffset - 1)
                            let containingTypeDeclaration = treeNode.GetContainingTypeDeclaration()
                            treeNode, containingTypeDeclaration.DeclaredElement :> _
                            
                        | None -> null, null
                    | _ -> null, null
                | _ -> null, null
            | _ -> null, null
            
        member __.FindContainingFunctionDeclarationBody(functionDeclarationNode: IFunctionDeclaration): ITreeNode = 
            null    