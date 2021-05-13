namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open JetBrains.Application.Infra
open JetBrains.DocumentModel
open JetBrains.DocumentModel.Impl
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CSharp.CodeCleanup
open JetBrains.ReSharper.Feature.Services.CodeCleanup
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util
open JetBrains.Util.Text

[<CodeCleanupModule>]
type FSharpReformatCode() =
    interface ICodeCleanupModule with
        member x.Name = "Reformat F#"
        member x.LanguageType = FSharpLanguage.Instance :> _
        member x.Descriptors = EmptyList.Instance :> _
        member x.IsAvailableOnSelection = true
        member x.SetDefaultSetting(_, _) = ()

        member x.IsAvailable(sourceFile: IPsiSourceFile) =
            sourceFile.PrimaryPsiLanguage :? FSharpLanguage

        member x.IsAvailable(profile: CodeCleanupProfile) =
            profile.GetSetting(ReformatCode.REFORMAT_CODE_DESCRIPTOR)

        member x.Process(sourceFile, rangeMarker, _, _, _) =
            let fsFile = sourceFile.FSharpFile
            if isNull fsFile then () else

            match fsFile.ParseTree with // todo: completion on enter after with
            | None -> ()
            | Some _ ->

            let filePath = sourceFile.GetLocation().FullPath
            let document = sourceFile.Document :?> DocumentBase
            let text = document.GetText()
            let checkerService = fsFile.CheckerService
            
            let solution = fsFile.GetSolution()
            let settings = sourceFile.GetSettingsStoreWithEditorConfig()
            let languageService = fsFile.Language.LanguageServiceNotNull()
            let formatter = languageService.CodeFormatter
            let codeFormatterProvider = solution.GetComponent<FantomasFormatterProvider>()

            let settings =
                formatter.GetFormatterSettings(solution, sourceFile, settings, false) :?> FSharpFormatSettingsKey

            let stamp = document.LastModificationStamp
            let modificationSide = TextModificationSide.NotSpecified
            let newLineText = sourceFile.DetectLineEnding().GetPresentation()
            let parsingOptions = checkerService.FcsProjectProvider.GetParsingOptions(sourceFile)

            let change = 
                if isNotNull rangeMarker then
                    try
                        let range = ofDocumentRange rangeMarker.DocumentRange
                        let formatted =
                            codeFormatterProvider.FormatSelection(filePath, range, text, settings, parsingOptions, newLineText)
                        let offset = rangeMarker.DocumentRange.StartOffset.Offset
                        let oldLength = rangeMarker.DocumentRange.Length
                        Some(DocumentChange(document, offset, oldLength, formatted, stamp, modificationSide))
                    with _ -> None
                else
                    let formatted =
                        codeFormatterProvider.FormatDocument(filePath, text, settings, parsingOptions, newLineText)
                    Some(DocumentChange(document, 0, text.Length, formatted, stamp, modificationSide))

            match change with
            | Some change ->
                use cookie = WriteLockCookie.Create()
                document.ChangeDocument(change, TimeStamp.NextValue)
                sourceFile.GetPsiServices().Files.CommitAllDocuments()
            | _ -> ()
