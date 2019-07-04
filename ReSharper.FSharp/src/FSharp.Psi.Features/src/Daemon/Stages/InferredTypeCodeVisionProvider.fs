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
        let returnTy = mfv.ReturnParameter.Type.Format symbolUse.DisplayContext

        let curriedParameterGroups =
            if mfv.IsPropertyGetterMethod then []
            else
              let groups =
                [ for group in mfv.CurriedParameterGroups ->
                    [ for p in group do
                        let ty = p.Type.Format symbolUse.DisplayContext
                        yield
                          p.DisplayName,
                          if p.Type.IsFunctionType then sprintf "(%s)" ty else ty ]]

              match groups with
              | [[]] when mfv.IsMember && (not mfv.IsPropertyGetterMethod) -> [["unit", "unit"]]
              | _ -> groups

        [ for curriedGroup in curriedParameterGroups do
              let group = [ for _name, ty in curriedGroup -> ty ] |> String.concat " * "
              if curriedGroup.Length = 1 then yield group
              else yield sprintf "(%s)" group

          yield returnTy ]
        |> String.concat " -> "

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
                let text = formatMemberOrFunctionOrValue (mfv, symbolUse)
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
