module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.UnusedOpensUtil

open System.Collections.Generic
open FSharp.Compiler.EditorServices
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

let [<Literal>] opName = "UnusedOpensStageProcess"

let getUnusedOpens (fsFile: IFSharpFile): IOpenStatement[] =
    let document = fsFile.GetSourceFile().Document

    let lines = Dictionary<int, string>()

    let getLine line =
        let line = line - 1
        use cookie = ReadLockCookie.Create()
        lines.GetOrCreateValue(line, fun () -> document.GetLineText(docLine line))

    let highlightings = List()
    match fsFile.GetParseAndCheckResults(false, opName) with
    | None -> EmptyArray.Instance
    | Some results ->

    let checkResults = results.CheckResults
    for range in UnusedOpens.getUnusedOpens(checkResults, getLine).RunAsTask() do
        match fsFile.GetNode<IOpenStatement>(document, range) with
        | null -> ()
        | openDirective ->
            // todo: remove this check after FCS update, https://github.com/dotnet/fsharp/pull/10510
            if isNull openDirective.TypeKeyword then
                highlightings.Add(openDirective)

    highlightings.AsArray()
