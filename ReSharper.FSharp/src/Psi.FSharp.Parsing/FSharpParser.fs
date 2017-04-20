namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.DataFlow
open JetBrains.ReSharper.Psi.FSharp
open JetBrains.ReSharper.Psi.FSharp.Tree
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Ast

type FSharpParser(file : IPsiSourceFile, checkerService : FSharpCheckerService, logger : ILogger) =
    member this.LogErrors errors =
        let messages = Array.map (fun (e : FSharpErrorInfo) -> e.Message) errors
        let filePath = file.GetLocation().FullPath
        logger.LogMessage(LoggingLevel.WARN, sprintf "%s: %s" filePath (StringUtil.Join(messages, "\n")))

    member private x.CreateTreeBuilder lexer parseTree lifetime =
        match file.PsiModule.IsMiscFilesProjectModule(), parseTree with
        | false, Some (ParsedInput.ImplFile (_)) ->
            FSharpImplTreeBuilder(file, lexer, parseTree, lifetime, logger) :> FSharpTreeBuilderBase
        | false, Some (ParsedInput.SigFile (_)) ->
            FSharpSigTreeBuilder(file, lexer, parseTree, lifetime, logger) :> FSharpTreeBuilderBase
        | _ ->
            // FCS could't parse the file, but we still want a correct IFile
            FSharpFakeTreeBuilder(file, lexer, lifetime, logger) :> FSharpTreeBuilderBase

    interface IParser with
        member this.ParseFile() =
            use lifetimeDefinition = Lifetimes.Define()
            let lifetime = lifetimeDefinition.Lifetime
            let parseResults = checkerService.ParseFSharpFile file
            if not <| parseResults.Errors.IsEmpty()
                then this.LogErrors parseResults.Errors

            let tokenBuffer = TokenBuffer(FSharpLexer(file.Document, checkerService.GetDefinedConstants file))
            let lexer = tokenBuffer.CreateLexer()
            let treeBuilder = this.CreateTreeBuilder lexer parseResults.ParseTree lifetime

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
