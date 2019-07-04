namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System
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
                let text = mfv.FullType.Format(symbolUse.DisplayContext)
                consumer.AddHighlighting(CodeInsightsHighlighting(range, text, "", "", x, null, null))
            | :? FSharpField as f ->
                let range = binding.GetNavigationRange()
                let text = f.FieldType.Format(symbolUse.DisplayContext)
                consumer.AddHighlighting(CodeInsightsHighlighting(range, text, "", "", x, null, null))
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
                let returnTy = mfv.ReturnParameter.Type.Format symbolUse.DisplayContext

                let curriedParameterGroups =
                    if mfv.IsPropertyGetterMethod then []
                    else
                      let groups =
                        mfv.CurriedParameterGroups
                        |> Seq.map (Seq.map (fun p -> p.DisplayName, p.Type.Format symbolUse.DisplayContext) >> Seq.toList)
                        |> Seq.toList

                      match groups with
                      | [[]] when mfv.IsMember && (not mfv.IsPropertyGetterMethod) -> [["unit", "unit"]]
                      | _ -> groups

                let text =
                    [ for curriedGroup in curriedParameterGroups do
                        yield
                          [ for name, ty in curriedGroup do
                              if not (String.IsNullOrWhiteSpace name) then
                                  yield sprintf "%s:%s" name ty
                              else
                                  yield ty ]
                          |> String.concat " * "

                      yield returnTy ]
                    |> String.concat " -> "

                consumer.AddHighlighting(CodeInsightsHighlighting(range, text, "", "", x, null, null))
            | _ -> ()

    interface ICodeInsightsProvider with
        member x.ProviderId = id
        member x.DisplayName = id
        member x.DefaultAnchor = CodeLensAnchorKind.Default
        member x.RelativeOrderings = [| CodeLensRelativeOrderingFirst() :> CodeLensRelativeOrdering |] :> _

        member x.IsAvailableIn _ = true

        member x.OnClick(_, _) = ()
        member x.OnExtraActionClick(_, _, _) = ()
