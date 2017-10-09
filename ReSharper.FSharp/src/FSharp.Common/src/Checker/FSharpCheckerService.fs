namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open JetBrains
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.Progress
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Psi.Modules
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

type Logger = Util.ILoggerEx
type LoggingLevel = Util.LoggingLevel

type FSharpParseAndCheckResults = 
    {
      ParseResults: FSharpParseFileResults
      ParseTree: Ast.ParsedInput
      CheckResults: FSharpCheckFileResults
    }

[<ShellComponent>]
type FSharpCheckerService(lifetime, logger: Util.ILogger, onSolutionCloseNotifier: OnSolutionCloseNotifier) =
    do System.Environment.SetEnvironmentVariable("FCS_CheckFileInProjectCacheSize", "100")
    let checker = lazy FSharpChecker.Create(projectCacheSize = 200, keepAllBackgroundResolutions = false,
                                            legacyReferenceResolver = MSBuildReferenceResolver.Resolver)
    do
        onSolutionCloseNotifier.SolutionIsAboutToClose.Advise(lifetime, fun _ ->
            checker.Value.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients())

    member val OptionsProvider: IFSharpProjectOptionsProvider = null with get, set
    member x.Checker = checker.Value

    member x.ParseFile([<NotNull>] file: IPsiSourceFile) =
        match x.OptionsProvider.GetParsingOptions(file) with
        | Some parsingOptions ->
            let filePath = file.GetLocation().FullPath
            let parsingOptions =
                if not (Array.isEmpty parsingOptions.SourceFiles) then parsingOptions
                else
                    let project  = file.GetProject().GetLocation().FullPath
                    Logger.LogMessage(logger, LoggingLevel.WARN, "Loading from caches, don't have source files for {0} yet.", project)
                    { parsingOptions with SourceFiles = [| filePath |] }
            let source = file.Document.GetText()
            try
                let parseResults = x.Checker.ParseFile(filePath, source, parsingOptions).RunAsTask() 
                Some parsingOptions, Some parseResults
            with
            | :? ProcessCancelledException -> reraise()
            | exn ->
                Util.Logging.Logger.LogException(exn)
                Logger.LogMessage(logger, LoggingLevel.WARN, sprintf "Parse file error, parsing options: %A" parsingOptions)
                Some parsingOptions, None
        | _ -> None, None

    member x.HasPairFile([<NotNull>] file: IPsiSourceFile) =
        x.OptionsProvider.HasPairFile(file)

    member x.GetDefines(sourceFile: IPsiSourceFile) =
        match x.OptionsProvider.GetParsingOptions(sourceFile) with
        | Some options -> options.ConditionalCompilationDefines
        | _ -> []

    member x.ParseAndCheckFile([<NotNull>] file: IPsiSourceFile, allowStaleResults) =
        match x.OptionsProvider.GetProjectOptions(file) with
        | Some options ->
            let path = file.GetLocation().FullPath
            if Array.isEmpty options.SourceFiles then
                Logger.LogMessage(logger, LoggingLevel.WARN, "Requested type check for {0}, but msbuild project doesn't have any source files yet", path)

            let source = file.Document.GetText()
            // todo: don't cancel the computation when file didn't change
            match x.Checker.ParseAndCheckDocument(path, source, options, allowStaleResults).RunAsTask() with
            | Some (parseResults, checkResults) when parseResults.ParseTree.IsSome ->
                Some { ParseResults = parseResults; ParseTree = parseResults.ParseTree.Value; CheckResults = checkResults }
            | _ -> None
        | _ -> None

    member x.InvalidateProject(project: FSharpProject) =
        x.Checker.InvalidateConfiguration(project.Options.Value)
