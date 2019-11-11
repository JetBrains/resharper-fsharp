namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open FSharp.Compiler.Text
open Fantomas
open Fantomas.FormatConfig
open JetBrains.Application.Infra
open JetBrains.DocumentModel
open JetBrains.DocumentModel.Impl
open JetBrains.ReSharper.Feature.Services.CSharp.CodeCleanup
open JetBrains.ReSharper.Feature.Services.CodeCleanup
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util
open JetBrains.Util.Text

[<CodeCleanupModule>]
type FSharpReformatCode() =
    interface ICodeCleanupModule with
        member x.LanguageType = FSharpLanguage.Instance :> _
        member x.Descriptors = EmptyList.Instance :> _
        member x.IsAvailableOnSelection = true
        member x.SetDefaultSetting(_, _) = ()
        member x.IsAvailable(sourceFile) = sourceFile.PrimaryPsiLanguage :? FSharpLanguage

        member x.Process(sourceFile, rangeMarker, profile, _) =
            if not (profile.GetSetting(ReformatCode.REFORMAT_CODE_DESCRIPTOR)) then () else

            let fsFile = sourceFile.FSharpFile
            if isNull fsFile then () else

            match fsFile.ParseTree with // todo: completion on enter after with
            | None -> ()
            | Some parseTree ->

            let filePath = sourceFile.GetLocation().FullPath
            let document = sourceFile.Document :?> DocumentBase
            let text = document.GetText()
            let source = SourceOrigin.SourceText(SourceText.ofString(document.GetText()))
            let checkerService = fsFile.CheckerService

            let settings = sourceFile.GetFormatterSettings(fsFile.Language) :?> FSharpFormatSettingsKey
            let formatConfig = { FormatConfig.Default with
                                     PageWidth = settings.WRAP_LIMIT
                                     IndentSpaceNum = settings.INDENT_SIZE
                                     ReorderOpenDeclaration = settings.ReorderOpenDeclarations
                                     SpaceBeforeColon = settings.SpaceBeforeColon
                                     SpaceAfterComma = settings.SpaceAfterComma
                                     SpaceAfterSemicolon = settings.SpaceAfterSemicolon
                                     IndentOnTryWith = settings.IndentOnTryWith
                                     SpaceAroundDelimiter = settings.SpaceAroundDelimiter
                                     KeepNewlineAfter = settings.PreserveEndOfLine }

            let stamp = document.LastModificationStamp
            let modificationSide = TextModificationSide.NotSpecified
            let newLineText = sourceFile.DetectLineEnding().GetPresentation()

            let change = 
                if isNotNull rangeMarker then
                    try
                        let range = ofDocumentRange rangeMarker.DocumentRange
                        let parsingOptions = checkerService.GetParsingOptions(sourceFile)
                        let checker = checkerService.Checker
                        let formatted =
                            CodeFormatter
                                .FormatSelectionAsync(filePath, range, source, formatConfig, parsingOptions, checker)
                                .RunAsTask()
                                .Replace("\r\n", newLineText)
                        let offset = rangeMarker.DocumentRange.StartOffset.Offset
                        let oldLength = rangeMarker.DocumentRange.Length
                        Some(DocumentChange(document, offset, oldLength, formatted, stamp, modificationSide))
                    with _ -> None
                else
                    let defines = checkerService.GetDefines(sourceFile)
                    let formatted =
                        CodeFormatter
                            .FormatASTAsync(parseTree, filePath, defines,  Some source, formatConfig)
                            .RunAsTask()
                            .Replace("\r\n", newLineText)
                    Some(DocumentChange(document, 0, text.Length, formatted, stamp, modificationSide))

            match change with
            | Some change ->
                use cookie = WriteLockCookie.Create()
                document.ChangeDocument(change, TimeStamp.NextValue)
                sourceFile.GetPsiServices().Files.CommitAllDocuments()
            | _ -> ()
