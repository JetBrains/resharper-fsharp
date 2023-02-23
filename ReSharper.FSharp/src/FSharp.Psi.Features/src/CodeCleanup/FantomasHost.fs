namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open System
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open JetBrains.Application.Settings
open JetBrains.Core
open JetBrains.DocumentModel
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.Rd.Tasks
open JetBrains.ReSharper.Plugins.FSharp.Fantomas.Client
open JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCleanup.FSharpEditorConfig
open JetBrains.Util

module internal Reflection =
    let formatSettingType = typeof<FSharpFormatSettingsKey>

    let getFieldValue obj fieldName =
        let field = formatSettingType.GetField(fieldName)
        if isNotNull field then field.GetValue(obj) else null


[<SolutionComponent>]
type FantomasHost(solution: ISolution, fantomasFactory: FantomasProcessFactory, fantomasDetector: FantomasDetector,
                  schema: SettingsSchema) =
    let solutionLifetime = solution.GetSolutionLifetimes().UntilSolutionCloseLifetime
    let mutable connection: FantomasConnection = null
    let mutable formatConfigFields: string[] = [||]
    let mutable formatterHostLifetime: LifetimeDefinition = null

    let toEditorConfigName name = $"{fSharpEditorConfigPrefix}{StringUtil.MakeUnderscoreCaseName(name)}"

    let isConnectionAlive () =
        isNotNull connection && connection.IsActive

    let terminateConnection () =
        if isConnectionAlive () then formatterHostLifetime.Terminate()

    let connect () =
        // TryRun synchronizes process creation and keeps track of its status
        fantomasDetector.TryRun(fun (path, version) ->
            if isConnectionAlive () then () else
            formatterHostLifetime <- Lifetime.Define(solutionLifetime)
            connection <- fantomasFactory.Create(formatterHostLifetime.Lifetime, version, path).Run()
            formatConfigFields <- connection.Execute(fun x -> connection.ProtocolModel.GetFormatConfigFields.Sync(Unit.Instance, RpcTimeouts.Maximal))
        )

    let toRdFcsRange (range: range) =
        RdFcsRange(range.FileName, range.StartLine, range.StartColumn, range.EndLine, range.EndColumn)

    let toRdFcsPos (caretPosition: DocumentCoords) =
        RdFcsPos(int caretPosition.Line, int caretPosition.Column)

    let toRdFormatSettings (settings: FSharpFormatSettingsKey) =
        [| for field in formatConfigFields ->
            let fieldName =
                match field with
                    | "IndentSize" -> "INDENT_SIZE"
                    | "MaxLineLength" -> "WRAP_LIMIT"
                    | x -> x
            let value =
                match Reflection.getFieldValue settings fieldName with
                | null -> null
                | x ->
                match schema.GetEntry(typeof<FSharpFormatSettingsKey>, fieldName) with
                | :? SettingsScalarEntry as entry when entry.RawDefaultValue <> x -> x
                | _ -> ""
            let value =
                if isNull value then settings.FantomasSettings.TryGet(toEditorConfigName fieldName)
                else value.ToString()
            if isNull value then "" else value |]

    let toRdFcsParsingOptions (options: FSharpParsingOptions) =
        let lightSyntax = Option.toNullable options.IndentationAwareSyntax
        RdFcsParsingOptions(Array.last options.SourceFiles, lightSyntax,
            List.toArray options.ConditionalDefines, options.IsExe, options.LangVersionText)

    do fantomasDetector.VersionToRun.Advise(solutionLifetime, fun _ -> terminateConnection ())

    member x.FormatSelection(filePath, range, source, settings, options, newLineText) =
        connect()
        let args =
            RdFantomasFormatSelectionArgs(toRdFcsRange range, filePath, source, toRdFormatSettings settings,
                toRdFcsParsingOptions options, newLineText, null)

        connection.Execute(fun () -> connection.ProtocolModel.FormatSelection.Sync(args, RpcTimeouts.Maximal))

    member x.FormatDocument(filePath, source, settings, options, newLineText, cursorPosition: DocumentCoords) =
        connect()
        let args =
            RdFantomasFormatDocumentArgs(filePath, source, toRdFormatSettings settings, toRdFcsParsingOptions options,
                newLineText, toRdFcsPos cursorPosition)

        connection.Execute(fun () -> connection.ProtocolModel.FormatDocument.Sync(args, RpcTimeouts.Maximal))

    /// For tests
    member x.Version() =
        connection.Execute(fun () -> connection.ProtocolModel.GetVersion.Sync(Unit.Instance, RpcTimeouts.Maximal))

    member x.Terminate() = terminateConnection ()

    member x.DumpRunOptions() =
        let versionToRun = fantomasDetector.VersionToRun.Value
        fantomasDetector.GetSettings()
        |> Seq.sortBy (fun x -> x.Key)
        |> Seq.map (fun x -> $"{x.Key}: Version = ({x.Value.Location}, {x.Value.Version}), Status = {x.Value.Status}")
        |> String.concat "\n"
        |> (+) $"Version to run: ({versionToRun.Location}, {versionToRun.Version})\n\n"
