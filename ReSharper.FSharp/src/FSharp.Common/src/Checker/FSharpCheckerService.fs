namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System
open System.Collections.Generic
open System.Linq
open System.Threading
open JetBrains
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.Progress
open JetBrains.Application.Threading
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.CSharp
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
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
    let checker = lazy FSharpChecker.Create(projectCacheSize = 200, keepAllBackgroundResolutions = false,
                                            legacyReferenceResolver = MSBuildReferenceResolver.Resolver)
    do
        onSolutionCloseNotifier.SolutionIsAboutToClose.Advise(lifetime, fun () ->
            checker.Value.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients())

    member val OptionsProvider: IFSharpProjectOptionsProvider = null with get, set
    member x.Checker = checker.Value

    member x.ParseFile([<NotNull>] file: IPsiSourceFile) =
        match x.OptionsProvider.GetParsingOptions(file) with
        | Some parsingOptions as options ->
            let filePath = file.GetLocation().FullPath
            let source = file.Document.GetText()
            try
                let parseResults = x.Checker.ParseFile(filePath, source, parsingOptions).RunAsTask() 
                options, Some parseResults
            with
            | :? ProcessCancelledException -> reraise()
            | exn ->
                Util.Logging.Logger.LogException(exn)
                Logger.LogMessage(logger, LoggingLevel.WARN, sprintf "Parse file error, parsing options: %A" parsingOptions)
                options, None
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
            let filePath = file.GetLocation().FullPath
            let source = file.Document.GetText()
            // todo: don't cancel the computation when file didn't change
            match x.Checker.ParseAndCheckDocument(filePath, source, options, allowStaleResults).RunAsTask() with
            | Some (parseResults, checkResults) when parseResults.ParseTree.IsSome ->
                Some { ParseResults = parseResults; ParseTree = parseResults.ParseTree.Value; CheckResults = checkResults }
            | _ -> None
        | _ -> None

    member x.InvalidateProject(project: FSharpProject) =
        x.Checker.InvalidateConfiguration(project.Options.Value)
