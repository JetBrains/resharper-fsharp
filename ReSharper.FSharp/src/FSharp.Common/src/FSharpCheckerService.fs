namespace JetBrains.ReSharper.Plugins.FSharp.Common.CheckerService

open System
open System.Collections.Generic
open System.Threading
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.Threading
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Common
open JetBrains.ReSharper.Plugins.FSharp.Common.ProjectOptions
open Microsoft.FSharp.Compiler.SourceCodeServices

[<ShellComponent>]
type FSharpCheckerService(lifetime, onSolutionCloseNotifier : OnSolutionCloseNotifier) as this =
    let checker = FSharpChecker.Create(projectCacheSize = 200, keepAllBackgroundResolutions = false)
    do
        onSolutionCloseNotifier.SolutionIsAboutToClose.Advise(lifetime, fun () ->
            checker.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients()
            this.OptionsProvider <- null)

    member val OptionsProvider : IFSharpProjectOptionsProvider = null with get, set
    member x.Checker = checker

    member x.GetDefines(file : IPsiSourceFile) =
        x.OptionsProvider.GetDefines file

    member x.ParseFile([<NotNull>] file : IPsiSourceFile) =
        match x.OptionsProvider.GetProjectOptions(file, checker, true) with
        | Some projectOptions as options ->
            let filePath = file.GetLocation().FullPath
            let source = file.Document.GetText()
            options, Some (checker.ParseFileInProject(filePath, source, projectOptions).RunAsTask())
        | _ -> None, None

    member x.CheckFile([<NotNull>] file : IFile, parseResults : FSharpParseFileResults, ?interruptChecker) =
        match file.GetSourceFile() with
        | sourceFile when isNotNull sourceFile ->
            match x.OptionsProvider.GetProjectOptions(sourceFile, checker, false) with
            | Some options ->
                let filePath = sourceFile.GetLocation().FullPath
                let source = sourceFile.Document.GetText()
                let checkAsync = checker.CheckFileInProject(parseResults, filePath, 0, source, options)
                match checkAsync.RunAsTask(?interruptChecker = interruptChecker) with
                | FSharpCheckFileAnswer.Succeeded results -> Some results
                | _ -> None
            | _ -> None
        | _ -> None
