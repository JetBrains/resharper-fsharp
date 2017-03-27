namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.DataFlow
open JetBrains.ReSharper.Psi.FSharp
open JetBrains.ReSharper.Psi.FSharp.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util
open Microsoft.FSharp.Compiler

type FSharpParser(file : IPsiSourceFile, checkerService : FSharpCheckerService, logger : ILogger) =
    
    member this.LogErrors errors =
        let messages = Array.map (fun (e : FSharpErrorInfo) -> e.Message) errors
        logger.LogMessage(LoggingLevel.ERROR, StringUtil.Join(messages, "\n"))

    interface IParser with
        member this.ParseFile() =
            use lifetimeDefinition = Lifetimes.Define()
            let parseResults = checkerService.ParseFSharpFile file
            match parseResults.ParseTree with
            | Some ast ->
                let defines = checkerService.GetDefinedConstants file
                let tokenBuffer = TokenBuffer(FSharpLexer(file.Document, defines))
                let treeBuilder = FSharpTreeBuilder(file, tokenBuffer.CreateLexer(), ast, lifetimeDefinition.Lifetime)
                match treeBuilder.CreateFSharpFile() with
                | :? IFSharpFile as fsFile ->
                    fsFile.TokenBuffer <- tokenBuffer
                    fsFile.ParseResults <- parseResults
                    fsFile.CheckerService <- checkerService
                    fsFile :> IFile
                | _ -> null
            | None ->
                this.LogErrors parseResults.Errors
                null
