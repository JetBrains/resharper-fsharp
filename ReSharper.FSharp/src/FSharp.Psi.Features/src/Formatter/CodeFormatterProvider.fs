namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open System
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open FSharp.ExternalFormatter.Protocol
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.Rd.Tasks
open JetBrains.Rider.FSharp.ExternalFormatter.Server

[<SolutionComponent>]
type CodeFormatterProvider(solution: ISolution, externalFormatterFactory: ExternalFormatterProcessFactory) =
    let mutable connection: ExternalFormatterConnection = null

    let isConnectionAlive () =
        isNotNull connection && connection.IsActive

    let connect () =
        if isConnectionAlive () then () else
        let formatterHostLifetime = Lifetime.Define(solution.GetLifetime())
        connection <- externalFormatterFactory.Create(formatterHostLifetime.Lifetime).Run()

    let execute (action: unit -> string) =
        connect ()
        connection.Execute(action)

    let convertRange (range: range) =
        RdRange(range.FileName, range.StartLine, range.StartColumn, range.EndLine, range.EndColumn)

    let convertFormatSettings (settings: FSharpFormatSettingsKey) =
        RdFormatConfig
            (settings.INDENT_SIZE, settings.WRAP_LIMIT, settings.SpaceBeforeParameter,
             settings.SpaceBeforeLowercaseInvocation, settings.SpaceBeforeUppercaseInvocation,
             settings.SpaceBeforeClassConstructor, settings.SpaceBeforeMember, settings.SpaceBeforeColon,
             settings.SpaceAfterComma, settings.SpaceBeforeSemicolon, settings.SpaceAfterSemicolon,
             settings.IndentOnTryWith, settings.SpaceAroundDelimiter, settings.MaxIfThenElseShortWidth,
             settings.MaxInfixOperatorExpression, settings.MaxRecordWidth, settings.MaxArrayOrListWidth,
             settings.MaxValueBindingWidth, settings.MaxFunctionBindingWidth,
             settings.MultilineBlockBracketsOnSameColumn, settings.NewlineBetweenTypeDefinitionAndMembers,
             settings.KeepIfThenInSameLine, settings.MaxElmishWidth, settings.SingleArgumentWebMode,
             settings.AlignFunctionSignatureToIndentation, settings.AlternativeLongMemberDefinitions)

    let convertParsingOptions (options: FSharpParsingOptions) =
        let lightSyntax =
            match options.LightSyntax with
            | Some x -> Nullable<bool> x
            | None -> Nullable<bool>()

        RdParsingOptions(options.SourceFiles, lightSyntax)

    member x.FormatSelection(filePath: string, range: range, source: string, settings: FSharpFormatSettingsKey,
                             options: FSharpParsingOptions) =
        let args = RdFormatSelectionArgs(filePath, convertRange range, source, convertFormatSettings settings,
                                         convertParsingOptions options)

        execute (fun () -> connection.ProtocolModel.FormatSelection.Sync(args, RpcTimeouts.Maximal))

    member x.FormatDocument(filePath: string, source: string, settings: FSharpFormatSettingsKey,
                            options: FSharpParsingOptions) =
        let args = RdFormatDocumentArgs(filePath, source, convertFormatSettings settings, convertParsingOptions options)

        execute (fun () -> connection.ProtocolModel.FormatDocument.Sync(args, RpcTimeouts.Maximal))
