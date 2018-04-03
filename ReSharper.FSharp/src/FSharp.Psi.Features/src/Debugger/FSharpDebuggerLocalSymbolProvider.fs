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

[<Language(typeof<FSharpLanguage>)>]
type FSharpDebuggerLocalSymbolProvider() =
    interface IDebuggerLocalSymbolProvider with
        member __.FindLocalDeclarationAt(file: IFile, range: DocumentRange, name: string): ITreeNode * IDeclaredElement =
            match file with
            | :? IFSharpFile as fsFile ->
                match fsFile.ParseResults with
                | Some parseResults -> 
                    match fsFile.FindTokenAt(range.StartOffset) with
                    | :? FSharpIdentifierToken as token ->
                        let pos = range.Document.GetPos(token.GetTreeEndOffset().Offset)
                        //match parseResults.ValidateBreakpointLocation(pos) with
                        //| Some range when range.StartLine - 1 = line -> ()
                        null, null
                    | _ -> null, null
                | _ -> null, null
            | _ -> null, null
            
        member __.FindContainingFunctionDeclarationBody(functionDeclarationNode: IFunctionDeclaration): ITreeNode = 
            null    