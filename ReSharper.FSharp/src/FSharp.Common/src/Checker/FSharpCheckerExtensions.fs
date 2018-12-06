namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System
open Microsoft.FSharp.Compiler.SourceCodeServices

module FSharpCheckerExtensions =
    let map (f: 'T -> 'U) (a: Async<'T>) : Async<'U> =
        async {
            let! a = a
            return f a
        }

    type CheckResults =
        | Ready of (FSharpParseFileResults * FSharpCheckFileResults) option
        | StillRunning of Async<(FSharpParseFileResults * FSharpCheckFileResults) option>

open FSharpCheckerExtensions

[<Extension; Sealed; AbstractClass>]
type FSharpCheckerExtensions =
    [<Extension>]
    static member ParseAndCheckDocument(checker: FSharpChecker, filePath: string, sourceText: string, options: FSharpProjectOptions, allowStaleResults: bool) =
        let parseAndCheckFile =
            async {
                let! parseResults, checkFileAnswer = checker.ParseAndCheckFileInProject(filePath, sourceText.GetHashCode(), sourceText, options)
                return
                    match checkFileAnswer with
                    | FSharpCheckFileAnswer.Aborted ->
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
                | Some parsedInput -> Some (parseResults, checkResults)
                | None -> None
            | None -> None

        if allowStaleResults then
            async {
                let! freshResults = tryGetFreshResultsWithTimeout()

                let! results =
                    match freshResults with
                    | Ready x -> async.Return x
                    | StillRunning worker ->
                        async {
                            match allowStaleResults, checker.TryGetRecentCheckResultsForFile(filePath, options) with
                            | true, Some (parseResults, checkFileResults, _) ->
                                return Some (parseResults, checkFileResults)
                            | _ ->
                                return! worker
                        }
                return bindParsedInput results
            }
        else parseAndCheckFile |> map bindParsedInput
