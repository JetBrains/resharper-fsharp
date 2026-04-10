namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.DisableWarning

open JetBrains.Application.UI.Controls.BulbMenu.Anchors
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.Intentions.ConfigureMenu
open JetBrains.ReSharper.Intentions.DisableWarning
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.Util

[<CustomHighlightingActionProvider(typeof<FSharpProjectFileType>)>]
type DisableWarningActionProvider(settingsManager: HighlightingSettingsManager) =
    interface IDisableWarningActionProvider with
        member this.GetActions(highlighting, highlightingRange, sourceFile, configureAnchor) =
            let psiFile = sourceFile.GetDominantPsiFile<FSharpLanguage>().As<IFSharpFile>()
            if isNull psiFile then EmptyList.Instance else

            let ranges =
                match highlighting with
                | :? IHighlightingWithSecondaryRanges as h ->
                    [| yield h.CalculateRange(); for range in h.CalculateSecondaryRanges() -> range |]
                | _ ->
                    [| highlightingRange |]

            let commentGroup = SubmenuAnchor(configureAnchor, SubmenuBehavior.ExecutableDuplicateFirst, ConfigureHighlightingAnchor.SuppressPosition)
            let disableAnchor = InvisibleAnchor(commentGroup)

            let severityId = highlighting.GetConfigurableSeverityId()
            let compilerId = settingsManager.GetCompilerIds(highlighting, FSharpLanguage.Instance, sourceFile) |> Seq.tryHead

            if compilerId.IsNone && isNull severityId then EmptyList.Instance else

            let warning =
                match compilerId with
                | Some compilerId -> Warning.Compiler(CompilerDiagnosticId(compilerId))
                | _ -> Warning.ReSharper(ReSharperDiagnosticId(severityId))

            [|
               match warning with
               | Warning.ReSharper diagnosticId ->
                   yield DisableWarningOnceAction(ranges, psiFile, diagnosticId).ToConfigureActionIntention(disableAnchor)
                   yield DisableAndRestoreWarningAction(ranges, psiFile, warning).ToConfigureActionIntention(disableAnchor)
                   yield DisableWarningInFileAction(psiFile, warning).ToConfigureActionIntention(disableAnchor)
                   
                   let disableAllAnchor = disableAnchor.CreateNext(separate = true)
                   yield DisableWarningInFileAction(psiFile, Warning.ReSharper(ReSharperDiagnosticId(ReSharperControlConstruct.DisableAllReSharperWarningsID))).ToConfigureActionIntention(disableAllAnchor)

               | Warning.Compiler _ ->
                   if FSharpLanguageLevel.isFSharp100Supported psiFile then
                       yield DisableAndRestoreWarningAction(ranges, psiFile, warning).ToConfigureActionIntention(disableAnchor)

                   yield DisableWarningInFileAction(psiFile, warning).ToConfigureActionIntention(disableAnchor)
            |]
