namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open Microsoft.FSharp.Compiler.SourceCodeServices

[<Extension; Sealed; AbstractClass>]
type FSharpCheckerExtensions() =
    static let [<Literal>] timeout = 2000

    [<Extension>]
    static member ParseAndCheckDocument
            (checker: FSharpChecker, filePath: string, sourceText: string, options: FSharpProjectOptions,
             allowStaleResults: bool) =

        let parseAndCheckFile = async {
            match! checker.ParseAndCheckFileInProject(filePath, 0, sourceText, options) with
            | _, FSharpCheckFileAnswer.Aborted -> return None
            | parseResults, FSharpCheckFileAnswer.Succeeded(checkResults) -> return Some (parseResults, checkResults) }

        async {
            let! token = Async.CancellationToken

            let task = Async.StartAsTask(parseAndCheckFile, cancellationToken = token)
            if task.Wait(timeout, token) then return task.Result else

            match checker.TryGetRecentCheckResultsForFile(filePath, options) with
            | Some (parseResults, checkFileResults, _) -> return Some (parseResults, checkFileResults)
            | None -> return! parseAndCheckFile }
