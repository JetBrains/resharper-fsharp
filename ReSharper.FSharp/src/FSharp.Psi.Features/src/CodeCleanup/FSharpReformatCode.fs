namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open JetBrains.Application.Infra
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.DocumentModel.Impl
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCleanup
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util.Text

[<CodeCleanupModule>]
type FSharpReformatCode() =
    let REFORMAT_CODE_DESCRIPTOR = CodeCleanupOptionDescriptor<bool>(
        "FSReformatCode",
        CodeCleanupLanguage("F#", 2),
        CodeCleanupOptionDescriptor.ReformatGroup,
        displayName = "Reformat code")

    interface IReformatCodeCleanupModule with
        member x.Name = "Reformat F#"
        member x.LanguageType = FSharpLanguage.Instance :> _
        member x.Descriptors = [| REFORMAT_CODE_DESCRIPTOR |]
        member x.IsAvailableOnSelection = true
        member x.SetDefaultSetting(profile, profileType) =
            match profileType with
            | CodeCleanupService.DefaultProfileType.FULL
            | CodeCleanupService.DefaultProfileType.REFORMAT
            | CodeCleanupService.DefaultProfileType.CODE_STYLE ->
                profile.SetSetting<bool>(REFORMAT_CODE_DESCRIPTOR, true)
            | _ -> 
                Assertion.Fail($"Unexpected cleanup profile type: {nameof(profileType)}")

        member x.IsAvailable(sourceFile: IPsiSourceFile) =
            sourceFile.PrimaryPsiLanguage :? FSharpLanguage

        member x.IsAvailable(profile: CodeCleanupProfile) =
            profile.GetSetting(REFORMAT_CODE_DESCRIPTOR)

        member x.Process(sourceFile, rangeMarker, _, _, _) =
            let fsFile = sourceFile.FSharpFile
            if isNull fsFile then () else

            match fsFile.ParseTree with
            | None -> ()
            | Some _ ->

            let filePath = sourceFile.GetLocation().FullPath
            let document = sourceFile.Document :?> DocumentBase
            let text = document.GetText()

            let solution = fsFile.GetSolution()
            let settings = sourceFile.GetSettingsStoreWithEditorConfig()
            let formatter = fsFile.Language.LanguageServiceNotNull().CodeFormatter
            let settings = formatter.GetFormatterSettings(solution, sourceFile, settings, false) :?> _
            let fantomasHost = solution.GetComponent<FantomasHost>()

            let stamp = document.LastModificationStamp
            let modificationSide = TextModificationSide.NotSpecified
            let newLineText = sourceFile.DetectLineEnding().GetPresentation()
            let parsingOptions = fsFile.CheckerService.FcsProjectProvider.GetParsingOptions(sourceFile)

            let change =
                if isNotNull rangeMarker then
                    try
                        let range = ofDocumentRange rangeMarker.DocumentRange
                        let formatted =
                            fantomasHost.FormatSelection(filePath, range, text, settings, parsingOptions, newLineText)
                        let offset = rangeMarker.DocumentRange.StartOffset.Offset
                        let oldLength = rangeMarker.DocumentRange.Length
                        Some(DocumentChange(document, offset, oldLength, formatted, stamp, modificationSide))
                    with _ -> None
                else
                    let formatted = fantomasHost.FormatDocument(filePath, text, settings, parsingOptions, newLineText)
                    Some(DocumentChange(document, 0, text.Length, formatted, stamp, modificationSide))

            match change with
            | Some(change) ->
                use cookie = WriteLockCookie.Create()
                document.ChangeDocument(change, TimeStamp.NextValue)
                sourceFile.GetPsiServices().Files.CommitAllDocuments()
            | _ -> ()
