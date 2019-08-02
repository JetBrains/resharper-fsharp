namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System.Text
open FSharp.Compiler.SourceCodeServices
open JetBrains.Application
open JetBrains.ReSharper.Daemon.CodeInsights
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Rider.Model
open JetBrains.Util


[<StaticSeverityHighlighting(
    Severity.INFO,
    HighlightingGroupIds.CodeInsightsGroup, 
    AttributeId = HighlightingGroupIds.CodeInsightsGroup,
    OverlapResolve = OverlapResolveKind.NONE)>]
type FSharpInferredTypeHighlighting(range, text, provider: ICodeInsightsProvider) =
    inherit CodeInsightsHighlighting(range, text, "", "", provider, null, null)

    interface IHighlightingWithTestOutput with
        member x.TestOutput = text


[<ShellComponent>]
type InferredTypeCodeVisionProvider() =
    let [<Literal>] id = "F# Inferred types"

    interface ICodeInsightsProvider with
        member x.ProviderId = id
        member x.DisplayName = id
        member x.DefaultAnchor = CodeLensAnchorKind.Default
        member x.RelativeOrderings = [| CodeLensRelativeOrderingFirst() :> CodeLensRelativeOrdering |] :> _

        member x.IsAvailableIn _ = true

        member x.OnClick(_, _) = ()
        member x.OnExtraActionClick(_, _, _) = ()


[<DaemonStage>]
type InferredTypeCodeVisionStage(provider: InferredTypeCodeVisionProvider) =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT && base.IsSupported(sourceFile, processKind)

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
        InferredTypeCodeVisionProviderProcess(fsFile, settings, daemonProcess, provider) :> _


and InferredTypeCodeVisionProviderProcess(fsFile, settings, daemonProcess, provider: ICodeInsightsProvider) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let append (stringBuilder: StringBuilder) (s: string) =
        stringBuilder.Append(s) |> ignore

    let formatMfv (symbolUse: FSharpSymbolUse) (mfv: FSharpMemberOrFunctionOrValue) =
        let displayContext = symbolUse.DisplayContext
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
                let isFunctionType = fsType.IsFunctionType

                if isFunctionType then append builder "("
                append builder (fsType.Format(displayContext))
                if isFunctionType then append builder ")"

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
        let headPattern = binding.HeadPattern
        if not headPattern.IsDeclaration then () else

        match headPattern with
        | :? ITopNamedPat
        | :? ITopLongIdentPat ->
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
        match box (decl.GetFSharpSymbolUse()) with
        | null -> ()
        | symbolUse ->

        let symbolUse = symbolUse :?> FSharpSymbolUse
        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv ->
            let text = formatMfv symbolUse mfv
            x.AddHighlighting(consumer, decl, text)
        | _ -> ()
