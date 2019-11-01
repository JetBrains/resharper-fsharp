namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open Fantomas
open Fantomas.FormatConfig
open JetBrains.Application.Infra
open JetBrains.DocumentModel
open JetBrains.DocumentModel.Impl
open JetBrains.ReSharper.Feature.Services.CSharp.CodeCleanup
open JetBrains.ReSharper.Feature.Services.CodeCleanup
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.Util
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

            match sourceFile.FSharpFile with
            | null -> ()
            | fsFile ->

            match fsFile.ParseTree with // todo: completion on enter after with
            | None -> ()
            | Some parseTree ->

            let filePath = sourceFile.GetLocation().FullPath
            let document = sourceFile.Document :?> DocumentBase
            let source = document.GetText()

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
                                     PreserveEndOfLine = settings.PreserveEndOfLine }

            let stamp = document.LastModificationStamp
            let modificationSide = TextModificationSide.NotSpecified
            let newLineText = sourceFile.DetectLineEnding().GetPresentation()

            let change = 
                if isNotNull rangeMarker then
                    try
                        let range = ofDocumentRange rangeMarker.DocumentRange
                        let formatted =
                            CodeFormatter
                                .FormatSelection(filePath, range, source, formatConfig)
                                .Replace("\r\n", newLineText)
                        let offset = rangeMarker.DocumentRange.StartOffset.Offset
                        let oldLength = rangeMarker.DocumentRange.Length
                        Some(DocumentChange(document, offset, oldLength, formatted, stamp, modificationSide))
                    with _ -> None
                else
                    let formatted =
                        CodeFormatter
                            .FormatAST(parseTree, filePath, Some source, formatConfig)
                            .Replace("\r\n", newLineText)
                    Some(DocumentChange(document, 0, source.Length, formatted, stamp, modificationSide))

            match change with
            | Some change ->
                document.ChangeDocument(change, TimeStamp.NextValue)
                sourceFile.GetPsiServices().Files.CommitAllDocuments()
            | _ -> ()
