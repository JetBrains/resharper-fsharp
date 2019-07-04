namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open FSharp.Compiler.Ast
open FSharp.Compiler.SourceCodeServices
open JetBrains.Annotations
open JetBrains.DocumentModel
open JetBrains.Lifetimes
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util

type FSharpParser(lexer: ILexer, document: IDocument, path: FileSystemPath, sourceFile: IPsiSourceFile,
                  checkerService: FSharpCheckerService, symbolsCache: IFSharpResolvedSymbolsCache) =

    let tryCreateTreeBuilder lexer lifetime =
        Option.bind (fun (parseResults: FSharpParseFileResults) ->
            parseResults.ParseTree |> Option.map (function
            | ParsedInput.ImplFile(ParsedImplFileInput(_,_,_,_,_,decls,_)) ->
                FSharpImplTreeBuilder(lexer, document, decls, lifetime) :> FSharpTreeBuilderBase
            | ParsedInput.SigFile(ParsedSigFileInput(_,_,_,_,sigs)) ->
                FSharpSigTreeBuilder(lexer, document, sigs, lifetime) :> FSharpTreeBuilderBase))

    let createFakeBuilder lexer lifetime =
        { new FSharpTreeBuilderBase(lexer, document, lifetime) with
            override x.CreateFSharpFile() =
                x.FinishFile(x.Mark(), ElementType.F_SHARP_IMPL_FILE) }

    new (lexer, [<NotNull>] sourceFile: IPsiSourceFile, checkerService, symbolsCache) =
        FSharpParser(lexer, sourceFile.Document, sourceFile.GetLocation(), sourceFile, checkerService, symbolsCache)

    new (lexer, document, checkerService, symbolsCache) =
        FSharpParser(lexer, document, FSharpParser.SandBoxPath, null, checkerService, symbolsCache)

    static member val SandBoxPath = FileSystemPath.Parse("Sandbox.fs")

    interface IFSharpParser with
        member this.ParseFile() =
            use lifetimeDefinition = Lifetime.Define()
            let lifetime = lifetimeDefinition.Lifetime

            let defines = checkerService.GetDefines(sourceFile)
            let parsingOptions = checkerService.GetParsingOptions(sourceFile)

            let lexer = FSharpPreprocessedLexerFactory(defines).CreateLexer(lexer).ToCachingLexer()
            let parseResults = checkerService.ParseFile(path, document, parsingOptions)

            let treeBuilder =
                tryCreateTreeBuilder lexer lifetime parseResults
                |> Option.defaultWith (fun _ -> createFakeBuilder lexer lifetime)

            treeBuilder.CreateFSharpFile(CheckerService = checkerService,
                                         ParseResults = parseResults,
                                         ResolvedSymbolsCache = symbolsCache) :> _

        member this.ParseExpression(chameleonExpr) =
            Lifetime.Using(fun lifetime ->
                let sourceFile = chameleonExpr.GetSourceFile()
                let projectedOffset = chameleonExpr.GetTreeStartOffset().Offset

                // todo: cover error cases where fsImplFile or multiple expressions may be returned
                let treeBuilder = FSharpImplTreeBuilder(lexer, sourceFile.Document, [], lifetime, projectedOffset)
                treeBuilder.ProcessTopLevelExpression(chameleonExpr.SynExpr)
                treeBuilder.GetTreeNode()) :?> ISynExpr
