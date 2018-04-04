namespace JetBrains.ReSharper.Plugins.FSharp.Services.Debugger

open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Debugger
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
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
                        let ast = sprintf "%+A" parseTree
                        let _x = ast
                        let pos = range.Document.GetPos(range.EndOffset.Offset)
                        let visitor =
                            { new AstTraversal.AstVisitorBase<_>() with
                                member this.VisitExpr(_, traverseSynExpr, defaultTraverse, expr) = defaultTraverse expr

//                                    member this.VisitBinding(defaultTraverse, (Binding(headPat = headPat) as synBinding)) =
//                                        match headPat with
//                                        | SynPat.LongIdent(lid,_,_,_,_,_) -> 
//                                            let pat = headPat
//                                            let _ = pat
//                                            Some lid.Range
//                                        | _ -> None
                                
                                member this.VisitLetOrUse(bindings, range) =
                                    bindings |> List.tryPick (fun (Binding(headPat = headPat) as binding) ->
                                        match headPat with
                                        | SynPat.Named(_, id, false, _, range) when id.idText = name -> 
                                            Some range
                                        | _ -> None 
                                    )
                            }
                        
                        match AstTraversal.Traverse(pos, parseTree, visitor) with
                        | Some r ->
                            let endOffset = range.Document.GetTreeEndOffset(r)
                            let treeNode = fsFile.FindTokenAt(endOffset)
                            treeNode, null
                            
                        | None -> null, null
                    | _ -> null, null
                | _ -> null, null
            | _ -> null, null
            
        member __.FindContainingFunctionDeclarationBody(functionDeclarationNode: IFunctionDeclaration): ITreeNode = 
            null    