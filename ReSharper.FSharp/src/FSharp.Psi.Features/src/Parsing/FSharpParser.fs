namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Syntax
open JetBrains.Annotations
open JetBrains.DocumentModel
open JetBrains.Lifetimes
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing

type FSharpParser(lexer: ILexer, document: IDocument, path: FileSystemPath, sourceFile: IPsiSourceFile,
        checkerService: FcsCheckerService, symbolsCache: IFSharpResolvedSymbolsCache) =

    let tryCreateTreeBuilder lexer lifetime =
        Option.map (fun (parseResults: FSharpParseFileResults) ->
            match parseResults.ParseTree  with
            | ParsedInput.ImplFile(ParsedImplFileInput(modules = decls)) ->
                FSharpImplTreeBuilder(lexer, document, decls, lifetime) :> FSharpTreeBuilderBase
            | ParsedInput.SigFile(ParsedSigFileInput(modules = sigs)) ->
                FSharpSigTreeBuilder(lexer, document, sigs, lifetime) :> FSharpTreeBuilderBase)

    let createFakeBuilder lexer lifetime =
        { new FSharpTreeBuilderBase(lexer, document, lifetime) with
            override x.CreateFSharpFile() =
                x.FinishFile(x.Mark(), ElementType.F_SHARP_IMPL_FILE) }

    let parseFile (noCache: bool) =
        use lifetimeDefinition = Lifetime.Define()
        let lifetime = lifetimeDefinition.Lifetime

        let parsingOptions = checkerService.FcsProjectProvider.GetParsingOptions(sourceFile)
        let defines = parsingOptions.ConditionalCompilationDefines

        let lexer = FSharpPreprocessedLexerFactory(defines).CreateLexer(lexer).ToCachingLexer()
        let parseResults = checkerService.ParseFile(path, document, parsingOptions, noCache)

        let language =
            match sourceFile with
            | null -> FSharpLanguage.Instance :> PsiLanguageType
            | sourceFile -> sourceFile.PrimaryPsiLanguage

        let treeBuilder =
            tryCreateTreeBuilder lexer lifetime parseResults
            |> Option.defaultWith (fun _ -> createFakeBuilder lexer lifetime)

        treeBuilder.CreateFSharpFile(FcsCheckerService = checkerService,
                                     ParseResults = parseResults,
                                     ResolvedSymbolsCache = symbolsCache,
                                     LanguageType = language)

    new (lexer, [<NotNull>] sourceFile: IPsiSourceFile, checkerService, symbolsCache) =
        let document = if isNotNull sourceFile then sourceFile.Document else null
        let path = if isNotNull sourceFile then sourceFile.GetLocation() else null
        FSharpParser(lexer, document, path, sourceFile, checkerService, symbolsCache)

    new (lexer, document, checkerService, symbolsCache) =
        FSharpParser(lexer, document, FSharpParser.SandBoxPath, null, checkerService, symbolsCache)

    static member val SandBoxPath = FileSystemPath.Parse("Sandbox.fs")

    interface IFSharpParser with
        member this.ParseFSharpFile(noCache) = parseFile noCache
        member this.ParseFile() = parseFile false :> _

        member this.ParseExpression(chameleonExpr: IChameleonExpression, syntheticDocument) =
            let isSyntheticDocument = isNotNull syntheticDocument
            let document = if isSyntheticDocument then syntheticDocument else chameleonExpr.GetSourceFile().Document

            let projectedOffset, lineShift =
                let projectedOffset = chameleonExpr.GetTreeStartOffset().Offset
                let offsetShift = projectedOffset - chameleonExpr.OriginalStartOffset

                if offsetShift = 0 && isSyntheticDocument then
                    projectedOffset, 0
                else
                    let startLine = document.GetCoordsByOffset(projectedOffset).Line
                    let lineShift = int startLine - chameleonExpr.SynExpr.Range.StartLine + 1

                    let lineStartShift = document.GetLineStartOffset(startLine) - chameleonExpr.OriginalLineStart
                    let projectedOffset = projectedOffset + lineStartShift - offsetShift
                    projectedOffset, lineShift

            Lifetime.Using(fun lifetime ->
                // todo: cover error cases where fsImplFile or multiple expressions may be returned
                let treeBuilder =
                    FSharpExpressionTreeBuilder(lexer, document, lifetime, projectedOffset, lineShift)

                treeBuilder.ProcessTopLevelExpression(chameleonExpr.SynExpr)
                treeBuilder.GetTreeNode()) :?> IFSharpExpression
