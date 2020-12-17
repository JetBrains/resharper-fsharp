[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Checker.FSharpCheckerExtensions

open System.Threading
open System.Threading.Tasks
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Text
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.Util.Logging

let map (f: 'T -> 'U) (a: Async<'T>) : Async<'U> =
    async {
        let! a = a
        return f a
    }

type CheckResults =
    | Ready of (FSharpParseFileResults * FSharpCheckFileResults) option
    | StillRunning of Task<(FSharpParseFileResults * FSharpCheckFileResults) option>

type FSharpChecker with
    member x.ParseAndCheckDocument(path, source: ISourceText, options, allowStale: bool, opName) =
        let version = source.GetHashCode()

        let parseAndCheckFile =
            async {
                let! parseResults, checkFileAnswer =
                    x.ParseAndCheckFileInProject(path, version, source, options, userOpName = opName)

                return
                    match checkFileAnswer with
                    | FSharpCheckFileAnswer.Aborted ->
                        if parseResults.ParseTree.IsNone then
                            let creationErrors = parseResults.Errors
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

        let bindParsedInput(results: (FSharpParseFileResults * FSharpCheckFileResults) option) =
            match results with
            | Some(parseResults, checkResults) when parseResults.ParseTree.IsSome ->
                Some (parseResults, checkResults)
            | _ -> None

        async {
            match x.TryGetRecentCheckResultsForFile(path, options, source) with
            | None ->
                // No stale results available, wait for fresh results
                return! parseAndCheckFile

            | Some (parseResults, checkFileResults, cachedVersion) when allowStale && cachedVersion = version ->
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
        }
        |> map bindParsedInput
