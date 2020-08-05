namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System.Text
open FSharp.Compiler.SourceCodeServices
open JetBrains.Application
open JetBrains.Application.UI.Components
open JetBrains.Application.UI.PopupLayout
open JetBrains.Application.UI.Tooltips
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon.CodeInsights
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Host.Features.Services
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Rider.Model
open JetBrains.Util

module FSharpInferredTypeHighlighting =
    let [<Literal>] Id = "CodeInsights"

[<StaticSeverityHighlighting(
    Severity.INFO, typeof<HighlightingGroupIds.CodeInsights>, 
    AttributeId = FSharpInferredTypeHighlighting.Id, OverlapResolve = OverlapResolveKind.NONE)>]
type FSharpInferredTypeHighlighting(range, text, provider: ICodeInsightsProvider) =
    inherit CodeInsightsHighlighting(range, text, "", "Copy inferred type", provider, null, null)

    interface IHighlightingWithTestOutput with
        member x.TestOutput = text


[<ShellComponent>]
type InferredTypeCodeVisionProvider() =
    let [<Literal>] id = "F# Inferred types"
    let [<Literal>] copiedText = "Inferred type copied to clipboard"

    interface ICodeInsightsProvider with
        member x.ProviderId = id
        member x.DisplayName = id
        member x.DefaultAnchor = CodeLensAnchorKind.Default
        member x.RelativeOrderings = [| CodeLensRelativeOrderingFirst() :> CodeLensRelativeOrdering |] :> _

        member x.IsAvailableIn _ = true

        member x.OnClick(highlighting, _) =
            let entry = highlighting.Entry.As<TextCodeLensEntry>()
            if isNull entry then () else

            let shell = Shell.Instance
            shell.GetComponent<Clipboard>().SetText(entry.Text)
            shell.GetComponent<ITooltipManager>().Show(copiedText, PopupWindowContextSource(fun _ ->
                let offset = highlighting.Range.StartOffset.Offset
                RiderEditorOffsetPopupWindowContext(offset) :> _)) |> ignore

        member x.OnExtraActionClick(_, _, _) = ()


[<DaemonStage>]
type InferredTypeCodeVisionStage(provider: InferredTypeCodeVisionProvider) =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT && base.IsSupported(sourceFile, processKind) &&
        not (sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>())

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
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
        if paramGroups.Count = 1 && paramGroups.[0].IsEmpty() && mfv.IsMember then "unit -> " + returnTypeStr else

        let builder = StringBuilder()
        let isSingleGroup = paramGroups.Count = 1

        for group in paramGroups do
            let addTupleParens = not isSingleGroup && group.Count > 1
            if addTupleParens then append builder "("

            let mutable isFirstParam = true
            for param in group do
                if not isFirstParam then append builder " * "

                let fsType = param.Type
                let addParens =
                    fsType.IsFunctionType ||
                    fsType.IsTupleType && group.Count > 1

                if addParens then append builder "("
                append builder (fsType.Format(displayContext))
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
        let consumer = new FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)
        fsFile.ProcessThisAndDescendants(Processor(x, consumer))
        committer.Invoke(DaemonStageResult(consumer.Highlightings))

    override x.VisitTopBinding(binding, consumer) =
        let headPattern = binding.HeadPattern.IgnoreInnerParens()
        if not headPattern.IsDeclaration then () else

        match headPattern with
        | :? ITopReferencePat
        | :? ITopParametersOwnerPat
        | :? ITopAsPat ->
            match box ((headPattern :?> IFSharpDeclaration).GetFSharpSymbolUse()) with
            | null -> ()
            | symbolUse ->

            let symbolUse = (symbolUse :?> FSharpSymbolUse)
            match symbolUse.Symbol with
            | :? FSharpMemberOrFunctionOrValue as mfv ->
                let text = formatMfv symbolUse mfv
                x.AddHighlighting(consumer, binding, text)

            | :? FSharpField as field ->
                let text = field.FieldType.Format(symbolUse.DisplayContext)
                x.AddHighlighting(consumer, binding, text)
            | _ -> ()
        | _ -> ()

    override x.VisitMemberDeclaration(decl, consumer) =
        if isNotNull (ObjExprNavigator.GetByMember(decl)) then () else

        match box (decl.GetFSharpSymbolUse()) with
        | null -> ()
        | symbolUse ->

        let symbolUse = symbolUse :?> FSharpSymbolUse
        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv ->
            let text = formatMfv symbolUse mfv
            x.AddHighlighting(consumer, decl, text)
        | _ -> ()
