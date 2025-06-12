namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.DisableWarning

open JetBrains.Application.UI.Controls.BulbMenu.Anchors
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.Intentions
open JetBrains.ReSharper.Intentions.DisableWarning
open JetBrains.ReSharper.Plugins.FSharp
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
            let severityId = highlighting.GetConfigurableSeverityId()
            if isNull severityId then EmptyList.Instance else

            let hasCompilerId =
                not (Seq.isEmpty (settingsManager.GetCompilerIds(highlighting, FSharpLanguage.Instance, sourceFile)))
            //TODO: suggest #nowarn
            if hasCompilerId then EmptyList.Instance else

            let psiFile = sourceFile.GetDominantPsiFile<FSharpLanguage>().As<IFSharpFile>()
            if isNull psiFile then EmptyList.Instance else

            let ranges =
                match highlighting with
                | :? IHighlightingWithSecondaryRanges as h ->
                    [| yield h.CalculateRange(); for range in h.CalculateSecondaryRanges() -> range |]
                | _ ->
                    [| highlightingRange |]

            let commentGroup = SubmenuAnchor(configureAnchor, SubmenuBehavior.ExecutableDuplicateFirst, ConfigureHighlightingAnchor.SuppressPosition)
            let disableByCommentAnchor = InvisibleAnchor(commentGroup)

            [|
               DisableWarningOnceAction(ranges, psiFile, severityId).ToConfigureActionIntention(disableByCommentAnchor)
               DisableAndRestoreWarningAction(ranges, psiFile,severityId).ToConfigureActionIntention(disableByCommentAnchor)
               DisableWarningInFileAction(psiFile, severityId).ToConfigureActionIntention(disableByCommentAnchor)

               let disableAllAnchor = disableByCommentAnchor.CreateNext(separate = true)
               DisableWarningInFileAction(psiFile, ReSharperControlConstruct.DisableAllReSharperWarningsID).ToConfigureActionIntention(disableAllAnchor)
            |]
