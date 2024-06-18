namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open JetBrains.Application.Infra
open JetBrains.Application.Parts
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.DocumentModel.Impl
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCleanup
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Resources
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.Util.Text

[<CodeCleanupModule(Instantiation.DemandAnyThreadSafe)>]
type FSharpReformatCode(textControlManager: ITextControlManager) =
    let REFORMAT_CODE_DESCRIPTOR = CodeCleanupOptionDescriptor<bool>(
        "FSharpReformatCode",
        CodeCleanupLanguage("F#", 2),
        CodeCleanupOptionDescriptor.ReformatGroup,
        typeof<Strings>)

    interface IReformatCodeCleanupModule with
        member x.Name = Strings.FSharpReformatCode_Name_Reformat_FSharp
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
            let settingsStore = sourceFile.GetSettingsStoreWithEditorConfig()
            let formatter = fsFile.Language.LanguageServiceNotNull().CodeFormatter
            let settings = formatter.GetFormatterSettings(solution, sourceFile, settingsStore, false) :?> _
            let fantomasHost = solution.GetComponent<FantomasHost>()

            let stamp = document.LastModificationStamp
            let modificationSide = TextModificationSide.NotSpecified
            let newLineText = sourceFile.DetectLineEnding().GetPresentation()
            let parsingOptions = fsFile.CheckerService.FcsProjectProvider.GetParsingOptions(sourceFile)

            try
                if isNotNull rangeMarker then
                    let range = ofDocumentRange rangeMarker.DocumentRange
                    let formatted = fantomasHost.FormatSelection(filePath, range, text, settings, parsingOptions, newLineText, settingsStore)
                    let offset = rangeMarker.DocumentRange.StartOffset.Offset
                    let oldLength = rangeMarker.DocumentRange.Length
                    let documentChange = DocumentChange(document, offset, oldLength, formatted, stamp, modificationSide)
                    use _ = WriteLockCookie.Create()
                    document.ChangeDocument(documentChange, TimeStamp.NextValue)
                    sourceFile.GetPsiServices().Files.CommitAllDocuments()
                else
                    let textControl = textControlManager.VisibleTextControls
                                      |> Seq.tryFind (fun c -> c.Document == document && c.Window.IsFocused.Value)
                    let cursorPosition = textControl |> Option.map (fun c -> c.Caret.Position.Value.ToDocLineColumn())
                    let formatResult = fantomasHost.FormatDocument(filePath, text, settings, parsingOptions, newLineText, cursorPosition, settingsStore)
                    let newCursorPosition = formatResult.CursorPosition

                    document.ReplaceText(document.DocumentRange, formatResult.Code)
                    sourceFile.GetPsiServices().Files.CommitAllDocuments()

                    if isNull textControl || isNull newCursorPosition then () else

                    // move cursor after current document transaction
                    let moveCursorLifetime = new LifetimeDefinition()
                    let codeCleanupService = solution.GetComponent<CodeCleanupService>()
                    codeCleanupService.WholeFileCleanupCompletedAfterSave.Advise(moveCursorLifetime.Lifetime, fun _ ->
                        moveCursorLifetime.Terminate()
                        textControl.Value.Caret.MoveTo(docLine newCursorPosition.Row,
                                                       docColumn newCursorPosition.Column,
                                                       CaretVisualPlacement.Generic))
            with _ -> ()
