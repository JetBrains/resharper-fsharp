namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open FSharp.Compiler.Symbols
open JetBrains.Application
open JetBrains.Application.Parts
open JetBrains.Application.Settings
open JetBrains.RdBackend.Common.Platform.CodeInsights
open JetBrains.ReSharper.Daemon.CodeInsights
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Common.ActionUtils
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Resources
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Rider.Model
open JetBrains.TextControl.DocumentMarkup.Adornments
open JetBrains.TextControl.DocumentMarkup.Adornments.IntraTextAdornments

module FSharpInferredTypeHighlighting =
    let [<Literal>] Id = "CodeInsights"
    let providerId = Strings.FSharpInferredTypeHighlighting_ProviderId
    let tooltipText = Strings.FSharpInferredTypeHighlighting_TooltipText

[<StaticSeverityHighlighting(
    Severity.INFO, typeof<HighlightingGroupIds.CodeInsights>,
    AttributeId = FSharpInferredTypeHighlighting.Id, OverlapResolve = OverlapResolveKind.NONE)>]
type FSharpInferredTypeHighlighting(range, text, provider: ICodeInsightsProvider) =
    inherit CodeInsightsHighlighting(range, text, "", FSharpInferredTypeHighlighting.tooltipText, provider, null, null)

    interface IHighlightingWithTestOutput with
        member x.TestOutput = text


[<ShellComponent(Instantiation.DemandAnyThreadSafe)>]
type InferredTypeCodeVisionProvider() =
    interface ICodeInsightsProvider with
        member x.ProviderId = FSharpInferredTypeHighlighting.providerId
        member x.DisplayName = FSharpInferredTypeHighlighting.providerId
        member x.DefaultAnchor = CodeVisionAnchorKind.Default
        member x.RelativeOrderings = [| CodeVisionRelativeOrderingFirst() :> CodeVisionRelativeOrdering |] :> _

        member x.IsAvailableIn _ = true

        member x.OnClick(highlighting, _, _) =
            let codeInsightsHighlighting = highlighting.CodeInsightsHighlighting
            let entry = codeInsightsHighlighting.Entry.As<TextCodeVisionEntry>()
            if isNull entry then () else

            copyToClipboard entry.Text highlighting.Highlighter

        member x.OnExtraActionClick(_, _, _) = ()


[<DaemonStage(Instantiation.DemandAnyThreadSafe, StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |])>]
type InferredTypeCodeVisionStage(provider: InferredTypeCodeVisionProvider) =
    inherit FSharpDaemonStageBase(true, false)

    override x.CreateStageProcess(fsFile, settings, daemonProcess, _) =
        InferredTypeCodeVisionProviderProcess(fsFile, settings, daemonProcess, provider) :> _


and InferredTypeCodeVisionProviderProcess(fsFile, settings, daemonProcess, provider: ICodeInsightsProvider) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    member private x.AddHighlighting(consumer: IHighlightingConsumer, node: ITreeNode, text) =
        let range = node.GetNavigationRange()
        consumer.AddHighlighting(FSharpInferredTypeHighlighting(range, text, provider))

    override x.Execute(committer) =
        let consumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)
        let settingsStore = x.DaemonProcess.ContextBoundSettingsStore

        let isDisabled =
            // todo: fix zoning?
            not Shell.Instance.IsTestShell &&

            settingsStore.GetIndexedValue((fun (key: CodeInsightsSettings) -> key.DisabledProviders), FSharpInferredTypeHighlighting.providerId) ||

            settingsStore.GetValue(fun (key: GeneralInlayHintsOptions) -> key.EnableInlayHints) &&
            settingsStore.GetValue(fun (key: FSharpTypeHintOptions) -> key.ShowTypeHintsForTopLevelMembers)
                         .EnsureInlayHintsDefault(settingsStore) <> PushToHintMode.Never

        if not isDisabled then
            fsFile.ProcessThisAndDescendants(Processor(x, consumer))

        committer.Invoke(DaemonStageResult(consumer.CollectHighlightings()))

    override x.VisitTopBinding(binding, consumer) =
        let headPattern = binding.HeadPattern.IgnoreInnerParens()
        let pattern = FSharpPatternUtil.ignoreInnerAsPatsToRight headPattern
        let refPat = pattern.IgnoreInnerParens().As<IReferencePat>()
        if isNull refPat || not refPat.IsDeclaration then () else

        let symbolUse = refPat.GetFcsSymbolUse()
        if isNull symbolUse then () else

        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv ->
            let text = formatMfv false emptyDisplayContext mfv
            x.AddHighlighting(consumer, binding, text)

        | :? FSharpField as field ->
            let text = field.FieldType.Format(symbolUse.DisplayContext)
            x.AddHighlighting(consumer, binding, text)
        | _ -> ()

    override x.VisitMemberDeclaration(decl, consumer) =
        if isNotNull (ObjExprNavigator.GetByMember(decl)) then () else

        let symbolUse = decl.GetFcsSymbolUse()
        if isNull symbolUse then () else

        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv ->
            let text = formatMfv false emptyDisplayContext mfv
            x.AddHighlighting(consumer, decl, text)
        | _ -> ()
