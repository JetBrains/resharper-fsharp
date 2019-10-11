[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Util.OpensUtil

open System
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.SourceCodeServices.ParsedInput
open JetBrains.Application.Settings
open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.Util
open JetBrains.Util.Text

let addOpen (coords: DocumentCoords) (fsFile: IFSharpFile) (settings: IContextBoundSettingsStore) (ns: string) =
    match fsFile.ParseTree with
    | None -> failwith "isNotNull ParseTree"
    | Some parseTree ->

    let line = int coords.Line + 1
    let document = fsFile.GetSourceFile().Document

    let insertionPoint =
        // todo: remove this check 
        if isNull settings || settings.GetValue(fun key -> key.TopLevelOpenCompletion) then TopLevel else Nearest

    let insertContext = findNearestPointToInsertOpenDeclaration line parseTree [||] insertionPoint
    let pos = adjustInsertionPoint (docLine >> document.GetLineText) insertContext

    let isSystem = ns.StartsWith("System.", StringComparison.Ordinal) || ns = "System"
    let openPrefix = String(' ', pos.Column) + "open "
    let textToInsert = openPrefix + ns

    let line = pos.Line - 1 |> max 0
    let lineToInsert =
        seq { line - 1 .. -1 .. 0 }
        |> Seq.takeWhile (fun i ->
            let lineText = document.GetLineText(docLine i)
            lineText.StartsWith(openPrefix) &&
            (textToInsert < lineText || isSystem && not (lineText.StartsWith("open System"))))
        |> Seq.tryLast
        |> Option.defaultValue line

    // add empty line after all open expressions if needed
    let insertEmptyLine = not (document.GetLineText(docLine line).IsNullOrWhitespace())

    let prevLineEndOffset =
        if lineToInsert > 0 then document.GetLineEndOffsetWithLineBreak(docLine (max 0 (lineToInsert - 1)))
        else 0

    let newLineText = document.GetPsiSourceFile(fsFile.GetSolution()).DetectLineEnding().GetPresentation()
    let emptyLine = if insertEmptyLine then newLineText else ""
    document.InsertText(prevLineEndOffset, textToInsert + newLineText + emptyLine)

