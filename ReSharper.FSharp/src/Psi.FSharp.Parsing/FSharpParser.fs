namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.DataFlow
open JetBrains.ReSharper.Psi.FSharp
open JetBrains.ReSharper.Psi.FSharp.Util
open JetBrains.ReSharper.Psi.FSharp.Tree
open JetBrains.ReSharper.Psi.Tree

type FSharpParser(file : IPsiSourceFile) =
    interface IParser with
        member this.ParseFile() =
            use lifetimeDefinition = Lifetimes.Define()
            let parseResults = FSharpCheckerUtil.ParseFSharpFile file
            match parseResults.ParseTree with
            | Some ast ->
                let defines = FSharpProjectOptionsProvider.GetDefinedConstants file
                let tokenBuffer = TokenBuffer(FSharpLexer(file.Document, defines))
                let treeBuilder = FSharpTreeBuilder(file, tokenBuffer.CreateLexer(), ast, lifetimeDefinition.Lifetime)
                match treeBuilder.CreateFSharpFile() with
                | :? IFSharpFile as fsFile ->
                    fsFile.TokenBuffer <- tokenBuffer
                    fsFile.ParseResults <- parseResults
                    fsFile :> IFile
                | _ -> null
            | None -> null
    