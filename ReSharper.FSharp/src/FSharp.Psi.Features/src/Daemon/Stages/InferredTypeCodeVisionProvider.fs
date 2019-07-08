namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Daemon.CodeInsights
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.Rider.Model

[<DaemonStage>]
type InferredTypeCodeVisionProvider() =
    inherit FSharpDaemonStageBase()

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
        InferredTypeCodeVisionProviderProcess(fsFile, settings, daemonProcess) :> _

and InferredTypeCodeVisionProviderProcess(fsFile, settings,  daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let [<Literal>] id = "Inferred types"

    let formatMemberOrFunctionOrValue (mfv: FSharpMemberOrFunctionOrValue, symbolUse: FSharpSymbolUse) =
        let returnTypeStr = mfv.ReturnParameter.Type.Format symbolUse.DisplayContext

        let parameterGroups =
            if mfv.IsPropertyGetterMethod then []
            else
              let groups =
                [ for group in mfv.CurriedParameterGroups ->
                    [ for p in group do
                        let typeStr = p.Type.Format symbolUse.DisplayContext
                        if p.Type.IsFunctionType then yield sprintf "(%s)" typeStr
                        else yield typeStr ]]

              match groups with
              | [[]] when mfv.IsMember -> [["unit"]]
              | _ -> groups

        [ for group in parameterGroups do
            let groupStr = group |> String.concat " * "
            if group.Length = 1 then yield groupStr
            else yield sprintf "(%s)" groupStr
          yield returnTypeStr ]
        |> String.concat " -> "

    member private x.AddHighlighting(consumer: IHighlightingConsumer, range, text) =
        consumer.AddHighlighting(CodeInsightsHighlighting(range, text, "", "", x, null, null))

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

            let symbolUse = symbolUse :?> FSharpSymbolUse

            match symbolUse.Symbol with
            | :? FSharpMemberOrFunctionOrValue as mfv ->
                let range = binding.GetNavigationRange()
                let text = formatMemberOrFunctionOrValue (mfv, symbolUse)
                x.AddHighlighting(consumer, range, text)

            | :? FSharpField as field ->
                let range = binding.GetNavigationRange()
                let text = field.FieldType.Format(symbolUse.DisplayContext)
                x.AddHighlighting(consumer, range, text)
            | _ -> ()
        | _ -> ()

    override x.VisitMemberDeclaration(decl, consumer) =
        match box ((decl :> IFSharpDeclaration).GetFSharpSymbolUse()) with
        | null -> ()
        | symbolUse ->
            let symbolUse = symbolUse :?> FSharpSymbolUse
            match symbolUse.Symbol with
            | :? FSharpMemberOrFunctionOrValue as mfv ->
                let range = decl.GetNavigationRange()
                let text = formatMemberOrFunctionOrValue (mfv, symbolUse)
                x.AddHighlighting(consumer, range, text)
            | _ -> ()

    interface ICodeInsightsProvider with
        member x.ProviderId = id
        member x.DisplayName = id
        member x.DefaultAnchor = CodeLensAnchorKind.Default
        member x.RelativeOrderings = [| CodeLensRelativeOrderingFirst() :> CodeLensRelativeOrdering |] :> _

        member x.IsAvailableIn _ = true

        member x.OnClick(_, _) = ()
        member x.OnExtraActionClick(_, _, _) = ()
