namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.UI.RichText

type ToRecursiveFunctionFix(warning: UndefinedNameError) =
    inherit QuickFixBase()

    let referenceExpr = warning.Reference.GetElement().As<IReferenceExpr>()
    let mutable chosenLetBindings = Unchecked.defaultof<_>

    let getContainingBindings (refExpr: IReferenceExpr) =
        let rec loop (node: ITreeNode) result =
            match node.GetContainingNode<ILetBindings>() with
            | null -> List.rev result
            | node -> loop node (node :: result)
        loop refExpr []

    let isSuitable (letBindings: ILetBindings) =
        if isNull referenceExpr then false else
        if letBindings.IsRecursive then false else

        letBindings.BindingsEnumerable
        |> Seq.tryHead
        |> Option.map (fun (binding: IBinding) ->
            match binding.HeadPattern with
            | :? ILongIdentPat as lidPat -> lidPat.SourceName = referenceExpr.ShortName
            | _ -> false)
        |> Option.defaultValue false

    let getNameRange (letBindings: ILetBindings) =
        [| letBindings.Bindings.[0].HeadPattern.GetNavigationRange() |]

    override x.Text = "Make " + referenceExpr.ShortName + " recursive"

    override x.IsAvailable _ =
        if not (isValid referenceExpr && isNull referenceExpr.Qualifier) then false else

        getContainingBindings referenceExpr
        |> List.filter isSuitable
        |> List.isEmpty
        |> not

    override x.Execute(solution, textControl) =
        let letBindings = getContainingBindings referenceExpr |> List.filter isSuitable

        let occurrences =
            letBindings
            |> List.map (fun letBindings ->
                let name = RichText(referenceExpr.ShortName)
                WorkflowPopupMenuOccurrence(name, RichText.Empty, letBindings, getNameRange))
            |> Array.ofSeq

        let occurrence =
            let popupMenu = solution.GetComponent<WorkflowPopupMenu>()
            popupMenu.ShowPopup(textControl.Lifetime, occurrences, CustomHighlightingKind.Other, textControl, null)

        if isNull occurrence then () else

        chosenLetBindings <- Seq.head occurrence.Entities
        base.Execute(solution, textControl)
    
    override x.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(referenceExpr.IsPhysical())
        chosenLetBindings.SetIsRecursive(true)
        null
