namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open JetBrains.DataFlow
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.Util
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.SourceCodeServices

type internal FSharpParser(file: IPsiSourceFile, checkerService: FSharpCheckerService, logger: ILogger) =
    let tryCreateTreeBuilder lexer lifetime =
        Option.bind (fun (parseResults: FSharpParseFileResults) ->
            parseResults.ParseTree |> Option.map (function
            | ParsedInput.ImplFile(ParsedImplFileInput(_,_,_,_,_,decls,_)) ->
                FSharpImplTreeBuilder(file, lexer, decls, lifetime) :> FSharpTreeBuilderBase
            | ParsedInput.SigFile(ParsedSigFileInput(_,_,_,_,sigs)) ->
                FSharpSigTreeBuilder(file, lexer, sigs, lifetime) :> FSharpTreeBuilderBase))

    interface IParser with
        member this.ParseFile() =
            use lifetimeDefintion = Lifetimes.Define()
            let lifetime = lifetimeDefintion.Lifetime
            let factory = FSharpPreprocessedLexerFactory(checkerService.GetDefines(file)) :> ILexerFactory
            let lexer = TokenBuffer(factory.CreateLexer(file.Document.Buffer)).CreateLexer()
            let parseResults = checkerService.ParseFile(file)
            let treeBuilder =
                tryCreateTreeBuilder lexer lifetime parseResults
                |> Option.defaultWith (fun _ ->
                    { new FSharpTreeBuilderBase(file, lexer, lifetime) with
                        override x.CreateFSharpFile() =
                            x.FinishFile(x.Builder.Mark(), ElementType.F_SHARP_IMPL_FILE) })

            treeBuilder.CreateFSharpFile(CheckerService = checkerService,
                                         ParseResults = parseResults) :> _
