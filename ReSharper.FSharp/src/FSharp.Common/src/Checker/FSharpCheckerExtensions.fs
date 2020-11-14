[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Checker.FSharpCheckerExtensions

open System
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
    | StillRunning of Async<(FSharpParseFileResults * FSharpCheckFileResults) option>

type FSharpChecker with
    member x.ParseAndCheckDocument(path, source: ISourceText, options, allowStale: bool, opName) =
        let parseAndCheckFile =
            async {
                let! parseResults, checkFileAnswer =
                    let version = source.GetHashCode()
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
                try
                    let! worker = Async.StartChild(parseAndCheckFile, 1000)
                    let! result = worker
                    return Ready result
                with :? TimeoutException ->
                    return StillRunning parseAndCheckFile
            }

        let bindParsedInput(results: (FSharpParseFileResults * FSharpCheckFileResults) option) =
            match results with
            | Some(parseResults, checkResults) ->
                match parseResults.ParseTree with
                | Some _ -> Some (parseResults, checkResults)
                | None -> None
            | None -> None

        if allowStale then
            async {
                let! freshResults = tryGetFreshResultsWithTimeout()

                let! results =
                    match freshResults with
                    | Ready x -> async.Return x
                    | StillRunning worker ->
                        async {
                            match allowStale, x.TryGetRecentCheckResultsForFile(path, options) with
                            | true, Some (parseResults, checkFileResults, _) ->
                                return Some (parseResults, checkFileResults)
                            | _ ->
                                return! worker
                        }
                return bindParsedInput results
            }
        else parseAndCheckFile |> map bindParsedInput
