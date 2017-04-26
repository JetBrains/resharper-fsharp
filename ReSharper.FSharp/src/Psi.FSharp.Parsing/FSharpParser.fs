namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open JetBrains.DataFlow
open JetBrains.Platform.ProjectModel.FSharp
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.FSharp
open JetBrains.ReSharper.Psi.FSharp.Tree
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpParser(file : IPsiSourceFile, checkerService : FSharpCheckerService, logger : ILogger) =
    member this.LogErrors errors =
        let messages = Array.map (fun (e : FSharpErrorInfo) -> e.Message) errors
        let filePath = file.GetLocation().FullPath
        logger.LogMessage(LoggingLevel.WARN, sprintf "%s: %s" filePath (StringUtil.Join(messages, "\n")))

    member private x.CreateTreeBuilder lexer (parseResults : FSharpParseFileResults option) lifetime options =
        match parseResults with
        | Some results ->
            let parseTree = results.ParseTree
            let isScript = file.LanguageType.Equals(FSharpScriptProjectFileType.Instance)
            match not isScript && file.PsiModule.IsMiscFilesProjectModule(), parseTree with
            | false, Some (ParsedInput.ImplFile (_)) ->
                FSharpImplTreeBuilder(file, lexer, parseTree, lifetime, logger) :> FSharpTreeBuilderBase
            | false, Some (ParsedInput.SigFile (_)) ->
                FSharpSigTreeBuilder(file, lexer, parseTree, lifetime, logger) :> FSharpTreeBuilderBase
            | _ ->
                // FCS could't parse the file, but we still want a correct IFile
                FSharpFakeTreeBuilder(file, lexer, lifetime, logger, options) :> FSharpTreeBuilderBase
        | _ ->
            // We didn't have valid project options
            FSharpFakeTreeBuilder(file, lexer, lifetime, logger, options) :> FSharpTreeBuilderBase

    interface IParser with
        member this.ParseFile() =
            use lifetimeDefinition = Lifetimes.Define()
            let lifetime = lifetimeDefinition.Lifetime
            let projectOptions = checkerService.GetProjectOptions file
            let parseResults = checkerService.ParseFSharpFile(file, projectOptions)
            if parseResults.IsSome &&  not <| parseResults.Value.Errors.IsEmpty()
                then this.LogErrors parseResults.Value.Errors

            let tokenBuffer = TokenBuffer(FSharpLexer(file.Document, checkerService.GetDefinedConstants file))
            let lexer = tokenBuffer.CreateLexer()
            let treeBuilder = this.CreateTreeBuilder lexer parseResults lifetime projectOptions

            match treeBuilder.CreateFSharpFile() with
            | :? IFSharpFile as fsFile ->
                if parseResults.IsSome then
                    fsFile.ParseResults <- parseResults.Value
                fsFile.TokenBuffer <- tokenBuffer
                fsFile.PreviousCheckResults <- checkerService.GetPreviousCheckResults file
                fsFile.CheckerService <- checkerService
                fsFile :> IFile
            | _ ->
                logger.LogMessage(LoggingLevel.ERROR, "FSharpTreeBuilder returned null")
                null
