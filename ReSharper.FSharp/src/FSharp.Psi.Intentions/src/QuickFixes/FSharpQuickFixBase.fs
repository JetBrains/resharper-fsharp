namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Env
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Intentions.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Rider.Backend.Env
open JetBrains.TextControl
open JetBrains.UI.RichText

[<AbstractClass>]
type FSharpQuickFixBase() =
    inherit QuickFixBase()

    abstract ExecutePsiTransaction: solution: ISolution -> unit
    default x.ExecutePsiTransaction _ = ()

    override x.ExecutePsiTransaction(solution, _) =
        use formatterCookie = FSharpExperimentalFeatureCookie.Create(ExperimentalFeature.Formatter)
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
type FSharpScopedQuickFixBase(contextNode: ITreeNode) =
    inherit ScopedQuickFixBase()

    abstract ExecutePsiTransaction: ISolution -> unit
    default x.ExecutePsiTransaction _ = ()

    override x.ExecutePsiTransaction(solution, _) =
        use formatterCookie = FSharpExperimentalFeatureCookie.Create(ExperimentalFeature.Formatter)
        x.ExecutePsiTransaction(solution)
        null

    override this.TryGetContextTreeNode() = contextNode


type IFSharpQuickFixUtilComponent =
    inherit IQuickFixUtilComponent

    abstract BindTo: FSharpSymbolReference * ITypeElement -> FSharpSymbolReference

[<Language(typeof<FSharpLanguage>)>]
[<ZoneMarker(typeof<ILanguageFSharpZone>, typeof<IResharperHostCoreFeatureZone>, typeof<IRiderFeatureEnvironmentZone>)>]
type FSharpQuickFixUtilComponent() =
    let [<Literal>] FcsOpName = "FSharpQuickFixUtilComponent.BindTo"

    member x.BindTo(reference: FSharpSymbolReference, typeElement: ITypeElement) =
        let referenceOwner = reference.GetElement()
        use writeCookie = WriteLockCookie.Create(referenceOwner.IsPhysical())

        FSharpReferenceBindingUtil.SetRequiredQualifiers(reference, typeElement, referenceOwner)
        if FSharpResolveUtil.resolvesToQualified typeElement reference false FcsOpName then reference else

        addOpens reference typeElement

    interface IFSharpQuickFixUtilComponent with
        member x.BindTo(reference, typeElement, _, _) =
            x.BindTo(reference :?> _, typeElement) :> _

        member x.AddImportsForExtensionMethod(reference, _) = reference

        member this.BindTo(reference, typeElement) =
            this.BindTo(reference :?> _, typeElement)
