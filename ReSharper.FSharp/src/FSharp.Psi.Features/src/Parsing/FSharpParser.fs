namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open JetBrains.DataFlow
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpParser(file: IPsiSourceFile, checkerService: FSharpCheckerService, logger) =
    let createTreeBuilder lexer lifetime options  =
        Option.bind (fun (parseResults: FSharpParseFileResults) ->
            parseResults.ParseTree |> Option.map (function
            | ParsedInput.ImplFile(_) as tree ->
                FSharpImplTreeBuilder(file, lexer, tree, lifetime, logger) :> FSharpTreeBuilderBase
            | ParsedInput.SigFile(_) as tree ->
                FSharpSigTreeBuilder(file, lexer, tree, lifetime, logger) :> FSharpTreeBuilderBase))
        >> Option.defaultWith (fun _ -> FSharpFakeTreeBuilder(file, lexer, lifetime, logger, options) :> _)

    interface IParser with
        member this.ParseFile() =
            let lifetime = Lifetimes.Define().Lifetime
            let options, parseResults = checkerService.ParseFile(file)
            let tokenBuffer = TokenBuffer(FSharpLexer(file.Document, checkerService.GetDefines(file)))
            let lexer = tokenBuffer.CreateLexer()
            let treeBuilder = createTreeBuilder lexer lifetime options parseResults

            match treeBuilder.CreateFSharpFile() with
            | :? IFSharpFile as fsFile ->
                fsFile.ParseResults <- parseResults
                fsFile.CheckerService <- checkerService
                fsFile.ActualTokenBuffer <- tokenBuffer
                fsFile :> _
            | _ ->
                logger.LogMessage(LoggingLevel.WARN, "FSharpTreeBuilder returned null")
                null
