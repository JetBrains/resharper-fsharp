namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open System.Text
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Symbols
open JetBrains.Application
open JetBrains.Application.Settings
open JetBrains.Application.UI.Components
open JetBrains.Application.UI.PopupLayout
open JetBrains.Application.UI.Tooltips
open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Platform.CodeInsights
open JetBrains.ReSharper.Daemon.CodeInsights
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.RdBackend.Common.Features.Services
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Resources
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Rider.Model
open JetBrains.TextControl.DocumentMarkup
open JetBrains.Util

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


[<ShellComponent>]
type InferredTypeCodeVisionProvider() =
    let typeCopiedTooltipText = Strings.InferredTypeCodeVisionProvider_TypeCopied_TooltipText

    interface ICodeInsightsProvider with
        member x.ProviderId = FSharpInferredTypeHighlighting.providerId
        member x.DisplayName = FSharpInferredTypeHighlighting.providerId
        member x.DefaultAnchor = CodeVisionAnchorKind.Default
        member x.RelativeOrderings = [| CodeVisionRelativeOrderingFirst() :> CodeVisionRelativeOrdering |] :> _

        member x.IsAvailableIn _ = true

        member x.OnClick(highlighting, _) =
            let codeInsightsHighlighting = highlighting.CodeInsightsHighlighting
            let entry = codeInsightsHighlighting.Entry.As<TextCodeVisionEntry>()
            if isNull entry then () else

            let shell = Shell.Instance
            shell.GetComponent<Clipboard>().SetText(entry.Text)
            let documentMarkupManager = shell.GetComponent<IDocumentMarkupManager>()
            shell.GetComponent<ITooltipManager>().Show(typeCopiedTooltipText, PopupWindowContextSource(fun _ ->
                let documentMarkup = documentMarkupManager.TryGetMarkupModel(codeInsightsHighlighting.Range.Document)
                if isNull documentMarkup then null else

                documentMarkup.GetFilteredHighlighters(FSharpInferredTypeHighlighting.providerId,
                    fun h -> highlighting.Equals(h.UserData))
                |> Seq.tryHead
                |> Option.map RiderEditorOffsetPopupWindowContext
                |> Option.defaultValue null :> _
            ))

        member x.OnExtraActionClick(_, _, _) = ()


[<DaemonStage>]
type InferredTypeCodeVisionStage(provider: InferredTypeCodeVisionProvider) =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT && base.IsSupported(sourceFile, processKind) &&
        not (sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>())

    override x.CreateStageProcess(fsFile, settings, daemonProcess, _) =
        InferredTypeCodeVisionProviderProcess(fsFile, settings, daemonProcess, provider) :> _


and InferredTypeCodeVisionProviderProcess(fsFile, settings, daemonProcess, provider: ICodeInsightsProvider) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let append (stringBuilder: StringBuilder) (s: string) =
        stringBuilder.Append(s) |> ignore

    let formatMfv (symbolUse: FSharpSymbolUse) (mfv: FSharpMemberOrFunctionOrValue) =
        let displayContext = symbolUse.DisplayContext.WithShortTypeNames(true)
        let returnTypeStr = mfv.ReturnParameter.Type.Format(displayContext)

        if mfv.IsPropertyGetterMethod then returnTypeStr else

        let paramGroups = mfv.CurriedParameterGroups
        if paramGroups.IsEmpty() then returnTypeStr else
        if paramGroups.Count = 1 && paramGroups[0].IsEmpty() && mfv.IsMember then "unit -> " + returnTypeStr else

        let builder = StringBuilder()
        let isSingleGroup = paramGroups.Count = 1

        for group in paramGroups do
            let addTupleParens = not isSingleGroup && group.Count > 1
            if addTupleParens then append builder "("

            let mutable isFirstParam = true
            for param in group do
                if not isFirstParam then append builder " * "

                let fcsType = param.Type
                let addParens =
                    fcsType.IsFunctionType ||
                    fcsType.IsTupleType && group.Count > 1

                if addParens then append builder "("
                append builder (fcsType.Format(displayContext))
                if addParens then append builder ")"

                isFirstParam <- false

            if addTupleParens then append builder ")"
            append builder " -> "

        append builder returnTypeStr
        builder.ToString()

    member private x.AddHighlighting(consumer: IHighlightingConsumer, node: ITreeNode, text) =
        let range = node.GetNavigationRange()
        consumer.AddHighlighting(FSharpInferredTypeHighlighting(range, text, provider))

    override x.Execute(committer) =
        let consumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)

        let isDisabled =
            // todo: fix zoning?
            not Shell.Instance.IsTestShell &&

            x.DaemonProcess.ContextBoundSettingsStore.GetIndexedValue(
                (fun (key: CodeInsightsSettings) -> key.DisabledProviders), FSharpInferredTypeHighlighting.providerId)

        if not isDisabled then
            fsFile.ProcessThisAndDescendants(Processor(x, consumer))

        committer.Invoke(DaemonStageResult(consumer.Highlightings))

    override x.VisitTopBinding(binding, consumer) =
        let headPattern = binding.HeadPattern.IgnoreInnerParens()
        let pattern = FSharpPatternUtil.ignoreInnerAsPatsToRight headPattern
        let refPat = pattern.IgnoreInnerParens().As<IReferencePat>()
        if isNull refPat || not refPat.IsDeclaration then () else

        let symbolUse = refPat.GetFcsSymbolUse()
        if isNull symbolUse then () else

        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv ->
            let text = formatMfv symbolUse mfv
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
            let text = formatMfv symbolUse mfv
            x.AddHighlighting(consumer, decl, text)
        | _ -> ()
