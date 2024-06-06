namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Syntax
open JetBrains.Annotations
open JetBrains.DocumentModel
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.TextControl

type FSharpParser(lexer: ILexer, document: IDocument, path: VirtualFileSystemPath, sourceFile: IPsiSourceFile,
        checkerService: FcsCheckerService, symbolsCache: IFcsResolvedSymbolsCache) =

    let tryCreateTreeBuilder lexer lifetime =
        Option.map (fun (parseResults: FSharpParseFileResults) ->
            match parseResults.ParseTree  with
            | ParsedInput.ImplFile(ParsedImplFileInput(contents = decls)) ->
                FSharpImplTreeBuilder(lexer, document, decls, lifetime, path) :> FSharpTreeBuilderBase
            | ParsedInput.SigFile(ParsedSigFileInput(contents = sigs)) ->
                FSharpSigTreeBuilder(lexer, document, sigs, lifetime, path) :> FSharpTreeBuilderBase)

    let createFakeBuilder lexer lifetime =
        { new FSharpTreeBuilderBase(lexer, document, lifetime, path) with
            override x.CreateFSharpFile() =
                x.FinishFile(x.Mark(), ElementType.F_SHARP_IMPL_FILE) }

    let parseFile (noCache: bool) =
        use lifetimeDefinition = Lifetime.Define()
        let lifetime = lifetimeDefinition.Lifetime

        let parsingOptions = checkerService.FcsProjectProvider.GetParsingOptions(sourceFile)
        let defines = parsingOptions.ConditionalDefines

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
        // During rename of type + file the source file returns the new path,
        // but the parsing/project options still have the old one. It doesn't seem to affect anything.

        let document = if isNotNull sourceFile then sourceFile.Document else null
        let path = if isNotNull sourceFile then sourceFile.GetLocation() else null
        FSharpParser(lexer, document, path, sourceFile, checkerService, symbolsCache)

    new (lexer, document, sourceFile: IPsiSourceFile, checkerService, symbolsCache, overrideExtension: string) =
        let path =
            if isNotNull overrideExtension && overrideExtension = ".fsi" then
                FSharpParser.SandBoxSignaturePath
            elif isNull overrideExtension && isNotNull sourceFile && sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>() then
                FSharpParser.SandBoxSignaturePath
            else
                FSharpParser.SandBoxPath

        FSharpParser(lexer, document, path, sourceFile, checkerService, symbolsCache)

    static member val SandBoxPath = VirtualFileSystemPath.Parse("Sandbox.fs", InteractionContext.SolutionContext)
    static member val SandBoxSignaturePath = VirtualFileSystemPath.Parse("Sandbox.fsi", InteractionContext.SolutionContext)

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
                    FSharpExpressionTreeBuilder(lexer, document, lifetime, path, projectedOffset, lineShift)

                treeBuilder.ProcessTopLevelExpression(chameleonExpr.SynExpr)
                treeBuilder.GetTreeNode()) :?> IFSharpExpression
