namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.BulbActions
open JetBrains.ReSharper.Feature.Services.BulbActions.Commands
open JetBrains.ReSharper.Feature.Services.BulbActions.Commands.Menu
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Intentions.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpResolveUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.UI.RichText

[<AbstractClass>]
type FSharpQuickFixBase() =
    inherit QuickFixBase()

    abstract ExecutePsiTransaction: solution: ISolution -> unit
    default x.ExecutePsiTransaction _ = ()

    override x.ExecutePsiTransaction(solution, _) =
        x.ExecutePsiTransaction(solution)
        null

    member x.SelectExpression(expressions: (IFSharpExpression * string) array, solution: ISolution,
            textControl: ITextControl) =
        let occurrences =
            expressions
            |> Array.map (fun (expr, text) ->
                let getRange (expr: ITreeNode) = [| expr.GetDocumentRange() |]
                WorkflowPopupMenuOccurrence(RichText(text), RichText.Empty, expr, getRange))

        let occurrence =
            let popupMenu = solution.GetComponent<WorkflowPopupMenu>()
            popupMenu.ShowPopup(textControl.Lifetime, occurrences, CustomHighlightingKind.Other, textControl, null)

        occurrence
        |> Option.ofObj
        |> Option.bind (fun occurrence -> occurrence.Entities |> Seq.tryHead)
        |> Option.toObj


[<AbstractClass>]
type FSharpModernQuickFixBase() =
    inherit ModernQuickFixBase()

    member x.SelectExpression(expressions: (IFSharpExpression * string) array, action) =
        let menuItems =
            expressions
            |> Array.map (fun (expr, text) ->
                let range = expr.GetDocumentRange()
                BulbActionCommandMenuItem<IFSharpExpression>(Text = text, Data = expr, Range = range)
            )

        BulbActionCommandSequence.From(
            BulbActionCommands.ShowMenu(menuItems, fun _ _ selectedExpr ->
                BulbActionCommands.ExecutePsiTransaction(fun _ _ ->
                    action selectedExpr
                    null
                )
            )
        )


[<AbstractClass>]
type FSharpScopedQuickFixBase(contextNode: ITreeNode) =
    inherit ModernScopedQuickFixBase()

    abstract ExecutePsiTransaction: ISolution -> unit
    default x.ExecutePsiTransaction _ = ()

    override x.ExecutePsiTransaction(solution, _) =
        x.ExecutePsiTransaction(solution)
        null

    override this.TryGetContextTreeNode() = contextNode

[<AbstractClass>]
type FSharpScopedNonIncrementalQuickFixBase(contextNode: ITreeNode) =
    inherit ModernScopedNonIncrementalQuickFixBase()

    abstract ExecutePsiTransaction: ISolution -> unit
    default x.ExecutePsiTransaction _ = ()

    override x.ExecutePsiTransaction(solution, _) =
        x.ExecutePsiTransaction(solution)
        null

    override this.TryGetContextTreeNode() = contextNode

type IFSharpQuickFixUtilComponent =
    inherit IQuickFixUtilComponent

    abstract BindTo: FSharpSymbolReference * ITypeElement -> FSharpSymbolReference

[<Language(typeof<FSharpLanguage>)>]
type FSharpQuickFixUtilComponent() =
    let [<Literal>] FcsOpName = "FSharpQuickFixUtilComponent.BindTo"

    member x.BindTo(reference: FSharpSymbolReference, typeElement: ITypeElement) =
        let referenceOwner = reference.GetElement()
        use writeCookie = WriteLockCookie.Create(referenceOwner.IsPhysical())

        reference.SetRequiredQualifiers(typeElement, referenceOwner)
        if resolvesToQualified typeElement reference false FcsOpName = Resolved then reference else

        addOpens reference typeElement

    interface IFSharpQuickFixUtilComponent with
        member x.BindTo(reference, typeElement, _, _) =
            x.BindTo(reference :?> _, typeElement) :> _

        member x.AddImportsForExtensionMethod(reference, _) = reference

        member this.BindTo(reference, typeElement) =
            this.BindTo(reference :?> _, typeElement)
