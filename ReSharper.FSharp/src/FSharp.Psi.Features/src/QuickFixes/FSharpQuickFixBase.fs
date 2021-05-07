namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Intentions.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.TextControl
open JetBrains.UI.RichText

[<AbstractClass>]
type FSharpQuickFixBase() =
    inherit QuickFixBase()

    abstract ExecutePsiTransaction: solution: ISolution -> unit
    default x.ExecutePsiTransaction _ = ()

    override x.ExecutePsiTransaction(solution, _) =
        use formatterCookie = FSharpExperimentalFeatures.EnableFormatterCookie.Create()
        x.ExecutePsiTransaction(solution)
        null

    member x.SelectExpression(expressions: (IFSharpExpression * string) array, solution: ISolution,
            textControl: ITextControl) =
        let occurrences =
            expressions
            |> Array.map (fun (expr, text) ->
                let getRange (expr: ITreeNode) = [| expr.GetNavigationRange() |]
                WorkflowPopupMenuOccurrence(RichText(text), RichText.Empty, expr, getRange))

        let occurrence =
            let popupMenu = solution.GetComponent<WorkflowPopupMenu>()
            popupMenu.ShowPopup(textControl.Lifetime, occurrences, CustomHighlightingKind.Other, textControl, null)

        occurrence
        |> Option.ofObj
        |> Option.bind (fun occurrence -> occurrence.Entities |> Seq.tryHead)
        |> Option.toObj


[<AbstractClass>]
type FSharpScopedQuickFixBase() =
    inherit ScopedQuickFixBase()

    abstract ExecutePsiTransaction: ISolution -> unit
    default x.ExecutePsiTransaction _ = ()

    override x.ExecutePsiTransaction(solution, _) =
        use formatterCookie = FSharpExperimentalFeatures.EnableFormatterCookie.Create()
        x.ExecutePsiTransaction(solution)
        null


type IFSharpQuickFixUtilComponent =
    inherit IQuickFixUtilComponent

    abstract BindTo: FSharpSymbolReference * ITypeElement -> FSharpSymbolReference
