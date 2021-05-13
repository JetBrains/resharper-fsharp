namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open System
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.Rd.Tasks
open JetBrains.ReSharper.Plugins.FSharp.Fantomas.Client
open JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol

[<SolutionComponent>]
type FantomasFormatterProvider(solution: ISolution, fantomasFactory: FantomasProcessFactory) =
    let mutable connection: FantomasConnection = null

    let isConnectionAlive () =
        isNotNull connection && connection.IsActive

    let connect () =
        if isConnectionAlive () then () else
        let formatterHostLifetime = Lifetime.Define(solution.GetLifetime())
        connection <- fantomasFactory.Create(formatterHostLifetime.Lifetime).Run()

    let execute (action: unit -> string) =
        connect ()
        connection.Execute(action)

    let convertRange (range: range) =
        RdFcsRange(range.FileName, range.StartLine, range.StartColumn, range.EndLine, range.EndColumn)

    let convertFormatSettings (settings: FSharpFormatSettingsKey) =
        RdFantomasFormatConfig
            (settings.INDENT_SIZE, settings.WRAP_LIMIT, settings.SpaceBeforeParameter,
             settings.SpaceBeforeLowercaseInvocation, settings.SpaceBeforeUppercaseInvocation,
             settings.SpaceBeforeClassConstructor, settings.SpaceBeforeMember, settings.SpaceBeforeColon,
             settings.SpaceAfterComma, settings.SpaceBeforeSemicolon, settings.SpaceAfterSemicolon,
             settings.IndentOnTryWith, settings.SpaceAroundDelimiter, settings.MaxIfThenElseShortWidth,
             settings.MaxInfixOperatorExpression, settings.MaxRecordWidth, settings.MaxArrayOrListWidth,
             settings.MaxValueBindingWidth, settings.MaxFunctionBindingWidth,
             settings.MultilineBlockBracketsOnSameColumn, settings.NewlineBetweenTypeDefinitionAndMembers,
             settings.KeepIfThenInSameLine, settings.MaxElmishWidth, settings.SingleArgumentWebMode,
             settings.AlignFunctionSignatureToIndentation, settings.AlternativeLongMemberDefinitions,
             settings.SemicolonAtEndOfLine)

    let convertParsingOptions (options: FSharpParsingOptions) =
        let lightSyntax =
            match options.LightSyntax with
            | Some x -> Nullable<bool> x
            | None -> Nullable<bool>()

        RdFcsParsingOptions(Array.last options.SourceFiles, lightSyntax,
                            List.toArray options.ConditionalCompilationDefines, options.IsExe)

    member x.FormatSelection(filePath, range, source, settings, options, newLineText) =
        let args = RdFormatSelectionArgs(convertRange range, filePath, source, convertFormatSettings settings,
                                         convertParsingOptions options, newLineText)

        execute (fun () -> connection.ProtocolModel.FormatSelection.Sync(args, RpcTimeouts.Maximal))

    member x.FormatDocument(filePath, source, settings, options, newLineText) =
        let args = RdFormatDocumentArgs(filePath, source, convertFormatSettings settings, convertParsingOptions options,
                                        newLineText)

        execute (fun () -> connection.ProtocolModel.FormatDocument.Sync(args, RpcTimeouts.Maximal))
