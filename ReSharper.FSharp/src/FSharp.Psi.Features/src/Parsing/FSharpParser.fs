namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open FSharp.Compiler.Ast
open FSharp.Compiler.SourceCodeServices
open JetBrains.Lifetimes
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

type FSharpParser(lexer: ILexer, sourceFile: IPsiSourceFile, checkerService: FSharpCheckerService,
                  resolvedSymbolsCache: IFSharpResolvedSymbolsCache) =

    let tryCreateTreeBuilder lexer lifetime =
        Option.bind (fun (parseResults: FSharpParseFileResults) ->
            parseResults.ParseTree |> Option.map (function
            | ParsedInput.ImplFile(ParsedImplFileInput(_,_,_,_,_,decls,_)) ->
                FSharpImplTreeBuilder(sourceFile, lexer, decls, lifetime) :> FSharpTreeBuilderBase
            | ParsedInput.SigFile(ParsedSigFileInput(_,_,_,_,sigs)) ->
                FSharpSigTreeBuilder(sourceFile, lexer, sigs, lifetime) :> FSharpTreeBuilderBase))

    let createFakeBuilder lexer lifetime =
        { new FSharpTreeBuilderBase(sourceFile, lexer, lifetime, 0) with
            override x.CreateFSharpFile() =
                x.FinishFile(x.Mark(), ElementType.F_SHARP_IMPL_FILE) }

    interface IFSharpParser with
        member this.ParseFile() =
            use lifetimeDefinition = Lifetime.Define()
            let lifetime = lifetimeDefinition.Lifetime

            let lexerFactory = FSharpPreprocessedLexerFactory(checkerService.GetDefines(sourceFile))
            let lexer = lexerFactory.CreateLexer(lexer).ToCachingLexer()
            let parseResults = checkerService.ParseFile(sourceFile)
            let treeBuilder =
                tryCreateTreeBuilder lexer lifetime parseResults
                |> Option.defaultWith (fun _ -> createFakeBuilder lexer lifetime)

            let fsFile =
                treeBuilder.CreateFSharpFile(CheckerService = checkerService,
                                             ParseResults = parseResults,
                                             ResolvedSymbolsCache = resolvedSymbolsCache)
            fsFile :> _

        member this.ParseExpression(chameleonExpr) =
            Lifetime.Using(fun lifetime ->
                let sourceFile = chameleonExpr.GetSourceFile()
                let projectedOffset = chameleonExpr.GetTreeStartOffset().Offset

                // todo: cover error cases where fsImplFile or multiple expressions may be returned
                let treeBuilder = FSharpImplTreeBuilder(sourceFile, lexer, [], lifetime, projectedOffset)
                treeBuilder.ProcessExpression(chameleonExpr.SynExpr)
                treeBuilder.GetTreeNode()) :?> ISynExpr
