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
        logger.LogMessage(LoggingLevel.WARN, sprintf "%s: %s" (file.GetLocation().FullPath) (StringUtil.Join(messages, "\n")))

    interface IParser with
        member this.ParseFile() =
            use lifetimeDefinition = Lifetimes.Define()
            let lifetime = lifetimeDefinition.Lifetime
            let parseResults = checkerService.ParseFSharpFile file
            this.LogErrors parseResults.Errors
            let tokenBuffer = TokenBuffer(FSharpLexer(file.Document, checkerService.GetDefinedConstants file))
            let treeBuilder = FSharpTreeBuilder(file, tokenBuffer.CreateLexer(), parseResults, lifetime)
            match treeBuilder.CreateFSharpFile() with
            | :? IFSharpFile as fsFile ->
                fsFile.TokenBuffer <- tokenBuffer
                fsFile.ParseResults <- parseResults
                fsFile.PreviousCheckResults <- checkerService.GetPreviousCheckResults file
                fsFile.CheckerService <- checkerService
                fsFile :> IFile
            | _ ->
                logger.LogMessage(LoggingLevel.ERROR, "FSharpTreeBuilder returned null")
                null