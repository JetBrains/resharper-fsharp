namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open JetBrains.DataFlow
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Common.CheckerService
open JetBrains.ReSharper.Psi.FSharp.Tree
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpParser(file : IPsiSourceFile, checkerService : FSharpCheckerService, logger) =
    member private x.CreateTreeBuilder lexer (parseResults : FSharpParseFileResults option) lifetime options =
        match options, parseResults with
        | Some options, Some results when results.ParseTree.IsSome ->
            let parseTree = results.ParseTree
            match parseTree with
            | Some (ParsedInput.ImplFile (_)) ->
                FSharpImplTreeBuilder(file, lexer, parseTree, lifetime, logger) :> FSharpTreeBuilderBase
            | Some (ParsedInput.SigFile (_)) ->
                FSharpSigTreeBuilder(file, lexer, parseTree, lifetime, logger) :> FSharpTreeBuilderBase
        | _ ->
            FSharpFakeTreeBuilder(file, lexer, lifetime, logger, options) :> FSharpTreeBuilderBase

    interface IParser with
        member this.ParseFile() =
            use lifetimeDefinition = Lifetimes.Define()
            let lifetime = lifetimeDefinition.Lifetime
            let options, parseResults = checkerService.ParseFile(file)
            let tokenBuffer = TokenBuffer(FSharpLexer(file.Document, checkerService.GetDefines(file)))
            let lexer = tokenBuffer.CreateLexer()
            let treeBuilder = this.CreateTreeBuilder lexer parseResults lifetime options

            match treeBuilder.CreateFSharpFile() with
            | :? IFSharpFile as fsFile ->
                if parseResults.IsSome then
                    fsFile.ParseResults <- parseResults.Value
                fsFile.TokenBuffer <- tokenBuffer
                fsFile.CheckerService <- checkerService
                fsFile :> IFile
            | _ ->
                logger.LogMessage(LoggingLevel.WARN, "FSharpTreeBuilder returned null")
                null
