namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

type ToRecursiveFunctionFix(warning: UndefinedNameError) =
    inherit FSharpQuickFixBase()

    let referenceExpr = warning.Reference.GetElement().As<IReferenceExpr>()
    let mutable chosenLetBindings = Unchecked.defaultof<_>

    let getContainingBindings (refExpr: IReferenceExpr) =
        let rec loop (node: ITreeNode) result =
            match node.GetContainingNode<ILetBindings>() with
            | null -> List.rev result
            | node -> loop node (node :: result)
        loop refExpr []

    let isApplicable (letBindings: ILetBindings) =
        if isNull referenceExpr then false else
        if letBindings.IsRecursive then false else

        letBindings.BindingsEnumerable
        |> Seq.tryHead
        |> Option.map (fun (binding: IBinding) ->
            match binding.HeadPattern with
            | :? IReferencePat as refPat -> refPat.SourceName = referenceExpr.ShortName && binding.HasParameters
            | _ -> false)
        |> Option.defaultValue false

    let getNameRange (letBindings: ILetBindings) =
        letBindings.Bindings[0].HeadPattern.GetDocumentRange()

    override x.Text = $"Make '{referenceExpr.ShortName}' recursive"

    override x.IsAvailable _ =
        if not (isValid referenceExpr && not referenceExpr.IsQualified) then false else

        getContainingBindings referenceExpr
        |> List.filter isApplicable
        |> List.isEmpty
        |> not

    override x.GetCommandSequence() =
        let letBindings = getContainingBindings referenceExpr |> List.filter isApplicable
        let name = referenceExpr.ShortName

        let occurrences =
            letBindings
            |> List.map (fun bindings -> bindings, $"{name} (line {bindings.Bindings[0].HeadPattern.StartLine})")
            |> Array.ofSeq

        x.ShowMenuAndExecute(occurrences, ToRecursiveLetBindingsAction.Execute, getNameRange)

    override x.ExecutePsiTransaction _ =
        ToRecursiveLetBindingsAction.Execute(chosenLetBindings)
