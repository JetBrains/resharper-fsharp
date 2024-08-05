namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open JetBrains.Application.Parts
open JetBrains.Application.Settings
open JetBrains.Core
open JetBrains.DocumentModel
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.Rd.Tasks
open JetBrains.ReSharper.Plugins.FSharp.Fantomas.Client
open JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCleanup.FSharpEditorConfig
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.EditorConfig
open JetBrains.Util

module internal Reflection =
    let formatSettingType = typeof<FSharpFormatSettingsKey>

    let getFieldValue obj fieldName =
        let field = formatSettingType.GetField(fieldName)
        if isNotNull field then field.GetValue(obj) else null


[<SolutionComponent(InstantiationEx.LegacyDefault)>]
type FantomasHost(solution: ISolution, fantomasFactory: FantomasProcessFactory, fantomasDetector: FantomasDetector,
                  schema: SettingsSchema, settingsStore: ISettingsStore) =
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

    let toRdFcsPos (caretPosition: DocumentCoords option) =
        match caretPosition with
        | Some caretPosition -> RdFcsPos(int caretPosition.Line, int caretPosition.Column)
        | None -> null

    let toRdFormatSettings (settings: FSharpFormatSettingsKey) (settingsStore: IContextBoundSettingsStore) =
        [| for field in formatConfigFields ->
            let fieldName =
                match field with
                | "IndentSize" -> "INDENT_SIZE"
                | "MaxLineLength" -> "WRAP_LIMIT"
                | "InsertFinalNewline" -> "LINE_FEED_AT_FILE_END"
                | x -> x
            let value =
                match Reflection.getFieldValue settings fieldName with
                | null -> null
                | x ->
                match schema.GetEntry(typeof<FSharpFormatSettingsKey>, fieldName) with
                | :? SettingsScalarEntry as entry ->
                    if entry.RawDefaultValue <> x then x else
                    let settingsStore = settingsStore.As<IContextBoundSettingsStoreImplementation>()
                    if isNull settingsStore then "" else
                    let layer = settingsStore.FindLayerWhereSettingValueComeFrom(entry, null)
                    if isNotNull layer && startsWith ConfigFileUtils.EditorConfigName layer.Name then x else ""
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

    member x.FormatSelection(filePath, range, source, settings, options, newLineText, settingsStore) =
        connect()
        let args =
            RdFantomasFormatSelectionArgs(toRdFcsRange range, filePath, source, toRdFormatSettings settings settingsStore,
                toRdFcsParsingOptions options, newLineText, null)

        connection.Execute(fun () -> connection.ProtocolModel.FormatSelection.Sync(args, RpcTimeouts.Maximal))

    member x.FormatDocument(filePath, source, settings, options, newLineText, cursorPosition: DocumentCoords option, settingsStore) =
        connect()
        let args =
            RdFantomasFormatDocumentArgs(filePath, source, toRdFormatSettings settings settingsStore, toRdFcsParsingOptions options,
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
