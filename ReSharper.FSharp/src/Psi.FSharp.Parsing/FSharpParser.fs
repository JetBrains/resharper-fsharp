namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open JetBrains.DataFlow
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
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
            match results.ParseTree with
            | Some(ParsedInput.ImplFile(_)) as treeOpt ->
                FSharpImplTreeBuilder(file, lexer, treeOpt, lifetime, logger) :> FSharpTreeBuilderBase, treeOpt
            | Some(ParsedInput.SigFile(_)) as treeOpt ->
                FSharpSigTreeBuilder(file, lexer, treeOpt, lifetime, logger) :> FSharpTreeBuilderBase, treeOpt
        | _ ->
            FSharpFakeTreeBuilder(file, lexer, lifetime, logger, options) :> FSharpTreeBuilderBase, None

    interface IParser with
        member this.ParseFile() =
            let lifetime = Lifetimes.Define().Lifetime
            let options, parseResults = checkerService.ParseFile(file)
            let tokenBuffer = TokenBuffer(FSharpLexer(file.Document, checkerService.GetDefines(file)))
            let lexer = tokenBuffer.CreateLexer()
            let treeBuilder, tree = this.CreateTreeBuilder lexer parseResults lifetime options

            match treeBuilder.CreateFSharpFile() with
            | :? IFSharpFile as fsFile ->
                fsFile.ParseResults <- parseResults
                fsFile.CheckerService <- checkerService
                fsFile.ActualTokenBuffer <- tokenBuffer
                fsFile :> IFile
            | _ ->
                logger.LogMessage(LoggingLevel.WARN, "FSharpTreeBuilder returned null")
                null
