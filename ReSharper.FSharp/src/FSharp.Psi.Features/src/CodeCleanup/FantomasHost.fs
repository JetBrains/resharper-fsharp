namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open JetBrains.Application.Notifications
open JetBrains.Core
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
type FantomasHost(solution: ISolution, fantomasFactory: FantomasProcessFactory,
                  notifications: UserNotifications, fantomasRunSettings: FantomasProcessSettings) =
    let mutable connection: FantomasConnection = null
    let mutable formatConfigFields: string[] = [||]
    let mutable formatterHostLifetime: LifetimeDefinition = null

    let toEditorConfigName name = $"{fSharpEditorConfigPrefix}{StringUtil.MakeUnderscoreCaseName(name)}"

    let isConnectionAlive () =
        isNotNull connection && connection.IsActive

    let terminateConnection () =
        if isConnectionAlive () then formatterHostLifetime.Terminate()

    // Formatting called on UI thread, so we don't need to synchronize it
    let connect () =
        if isConnectionAlive () then () else

        fantomasRunSettings.TryRun(fun { Path = path } ->
            formatterHostLifetime <- Lifetime.Define(solution.GetLifetime())
            connection <- fantomasFactory.Create(formatterHostLifetime.Lifetime, path).Run()
            formatConfigFields <- connection.Execute(fun x -> connection.ProtocolModel.GetFormatConfigFields.Sync(Unit.Instance))
        )

    let convertRange (range: range) =
        RdFcsRange(range.FileName, range.StartLine, range.StartColumn, range.EndLine, range.EndColumn)

    let convertFormatSettings (settings: FSharpFormatSettingsKey) =
        [| for field in formatConfigFields ->
            let fieldName =
                match field with
                    | "IndentSize" -> "INDENT_SIZE"
                    | "MaxLineLength" -> "WRAP_LIMIT"
                    | x -> x
            let value = Reflection.getFieldValue settings fieldName
            let value =
                if isNull value then settings.FantomasSettings.TryGet(toEditorConfigName fieldName)
                else value.ToString()
            if isNull value then "" else value |]

    let convertParsingOptions (options: FSharpParsingOptions) =
        let lightSyntax = Option.toNullable options.LightSyntax
        RdFcsParsingOptions(Array.last options.SourceFiles, lightSyntax,
            List.toArray options.ConditionalCompilationDefines, options.IsExe, options.LangVersionText)

    do fantomasRunSettings.SelectedVersion.Advise(solution.GetLifetime(), fun _ -> terminateConnection ())

    member x.FormatSelection(filePath, range, source, settings, options, newLineText) =
        let args =
            RdFantomasFormatSelectionArgs(convertRange range, filePath, source, convertFormatSettings settings,
                convertParsingOptions options, newLineText)

        connect()
        connection.Execute(fun () -> connection.ProtocolModel.FormatSelection.Sync(args, RpcTimeouts.Maximal))

    member x.FormatDocument(filePath, source, settings, options, newLineText) =
        let args =
            RdFantomasFormatDocumentArgs(filePath, source, convertFormatSettings settings, convertParsingOptions options,
                newLineText)

        connect()
        connection.Execute(fun () -> connection.ProtocolModel.FormatDocument.Sync(args, RpcTimeouts.Maximal))
