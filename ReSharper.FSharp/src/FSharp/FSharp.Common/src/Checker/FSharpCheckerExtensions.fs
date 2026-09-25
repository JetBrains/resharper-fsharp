#nowarn FS0057

[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Checker.FSharpCheckerExtensions

open System.Threading
open System.Threading.Tasks
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.CodeAnalysis.ProjectSnapshot
open FSharp.Compiler.Text
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.Util.Logging

type CheckResults =
    | Ready of (FSharpParseFileResults * FSharpCheckFileResults) option
    | StillRunning of Task<(FSharpParseFileResults * FSharpCheckFileResults) option>

type FSharpChecker with
    member x.ParseAndCheckDocument(sourceFile: IPsiSourceFile, fcsProject: FcsProject, allowStale, opName) =
        let path = sourceFile.GetLocation().FullPath
        let source = sourceFile.Document.GetText()

        let options =
            match fcsProject.Options with
            | FcsProjectSnapshot projectSnapshot ->
                let currentFileSnapshot = FSharpFileSnapshot.CreateFromString(path, source)
                FcsProjectSnapshot(projectSnapshot.Replace([currentFileSnapshot]))
            | options -> options

        let parseAndCheckFile =
            async {
                let! parseResults, checkFileAnswer =
                    match options with
                    | FcsProjectOptions.FcsProjectOptions(options, _) ->
                        let source = SourceText.ofString(source)
                        //TODO: getHashCode is not required 
                        x.ParseAndCheckFileInProject(path, source.GetHashCode(), source, options, userOpName = opName)

                    | FcsProjectSnapshot projectSnapshot ->
                        x.ParseAndCheckFileInProject(path, projectSnapshot, userOpName = opName)

                return
                    match checkFileAnswer with
                    | FSharpCheckFileAnswer.Aborted ->
                        let creationErrors = parseResults.Diagnostics
                        if not (Array.isEmpty creationErrors) then
                            let logger = Logger.GetLogger<CheckResults>()
                            logErrors logger "FCS aborted" creationErrors

                        None
                    | FSharpCheckFileAnswer.Succeeded(checkFileResults) ->
                        Some (parseResults, checkFileResults)
            }

        let tryGetFreshResultsWithTimeout() : Async<CheckResults> =
            async {
                use cts = new CancellationTokenSource()
                let! t = Async.StartChildAsTask parseAndCheckFile
                use timer = Task.Delay(1000, cts.Token)
                let! completed = Async.AwaitTask(Task.WhenAny(t, timer))
                if completed = (t :> Task) then
                    cts.Cancel ()
                    let! result = Async.AwaitTask t
                    return Ready result
                else
                    return StillRunning t
            }

        async {
            match options with
            | FcsProjectOptions(options, _) ->
                let source = SourceText.ofString(sourceFile.Document.GetText())
                let version = source.GetHashCode()
                match x.TryGetRecentCheckResultsForFile(path, options, source) with
                | None ->
                    // No stale results available, wait for fresh results
                    return! parseAndCheckFile

                //TODO: allowStale?
                | Some (parseResults, checkFileResults, cachedVersion) when allowStale && cachedVersion = int64 version ->
                    // Avoid queueing on the reactor thread by using the recent results
                    return Some (parseResults, checkFileResults)

                | Some (staleParseResults, staleCheckFileResults, _) ->

                match! tryGetFreshResultsWithTimeout() with
                | Ready x ->
                    // Fresh results were ready quickly enough
                    return x

                | StillRunning _ when allowStale ->
                    // Still waiting for fresh results - just use the stale ones for now
                    return Some (staleParseResults, staleCheckFileResults)

                | StillRunning worker ->
                    return! Async.AwaitTask worker

            | FcsProjectSnapshot projectSnapshot ->
                match x.TryGetRecentCheckResultsForFile(path, projectSnapshot, userOpName = opName) with
                | None ->
                    // No stale results available, wait for fresh results
                    return! parseAndCheckFile

                | result ->
                    return result
        }
