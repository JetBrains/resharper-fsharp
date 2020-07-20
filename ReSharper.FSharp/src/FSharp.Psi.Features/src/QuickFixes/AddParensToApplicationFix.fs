namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ProjectModel
open JetBrains.UI.RichText
open JetBrains.Util
open System

type AddParensToApplicationFix(error: NotAFunctionError) =
    inherit FSharpQuickFixBase()

    let [<Literal>] multilineSuffix = " ..."
    let [<Literal>] quickFixText = "Add parens to application"
    let displayNameMaxLength = 70
    let errorPrefixApp = error.PrefixApp
    let mutable prefixAppWithArgsToApply = (null, [])
    
    let normalizeDisplayText (text: string) =
        if text.Length <= displayNameMaxLength then text else text.Substring(0, displayNameMaxLength) + multilineSuffix

    let getParentPrefixApp (expr: IFSharpExpression) nestingLevel =
        let rec getParentPrefixAppRec (expr: IFSharpExpression) i =
            let parentPrefixApp = PrefixAppExprNavigator.GetByFunctionExpression(expr)
            if i + 1 <= nestingLevel then getParentPrefixAppRec parentPrefixApp (i + 1) else parentPrefixApp

        getParentPrefixAppRec expr 1

    let rec createPrefixAppExprTree (factory: IFSharpElementFactory) (expr: IFSharpExpression) args =
        match args with
        | head :: tail ->
            let newAppExpr = factory.CreateAppExpr(expr, head, true)
            createPrefixAppExprTree factory newAppExpr tail
        | [] -> expr

    let countArgs fsharpType =
        let rec countArgsRec (fsharpType: FSharpType) count =
            let functionCandidate = fsharpType.GenericArguments.[1]
            if functionCandidate.IsFunctionType then countArgsRec functionCandidate count + 1 else count

        countArgsRec fsharpType 1

    let tryFindPrefixAppsWithoutParens prefixAppExpr =
        let rec collectAppliedExprsRec (prefixAppExpr : IPrefixAppExpr) (prefixAppDataAcc: (_ * int * _ list) list) (appliedExprsAcc: _ list) =
            let argExprFcsType = prefixAppExpr.ArgumentExpression.IgnoreInnerParens().TryGetFSharpType()
            let expectedArgsCount = if argExprFcsType != null && argExprFcsType.IsFunctionType then Some(countArgs argExprFcsType) else None
            let isPrefixAppWithoutParens = match expectedArgsCount with Some x -> x <= appliedExprsAcc.Length | _ -> false
            let prefixAppDataAcc =
                if isPrefixAppWithoutParens then (prefixAppExpr.ArgumentExpression, expectedArgsCount.Value, appliedExprsAcc) :: prefixAppDataAcc
                else prefixAppDataAcc
            match prefixAppExpr.FunctionExpression.IgnoreInnerParens() with
            | :? IPrefixAppExpr as appExpr -> collectAppliedExprsRec appExpr prefixAppDataAcc (prefixAppExpr.ArgumentExpression :: appliedExprsAcc)   
            | _ -> prefixAppDataAcc

        collectAppliedExprsRec prefixAppExpr [] []

    let prefixAppsData = tryFindPrefixAppsWithoutParens errorPrefixApp

    override x.Text = quickFixText

    override x.IsAvailable _ =
        match prefixAppsData with
        | [] -> false
        | list -> list |> List.forall (fun (prefixApp, _, _) -> isValid prefixApp)

    override x.Execute(solution, textControl) =
        let popupMenu = solution.GetComponent<WorkflowPopupMenu>()

        let getPrefixAppRange (data: IFSharpExpression * int * IFSharpExpression list) =
            let (prefixApp, _, _) = data
            [| prefixApp.GetNavigationRange() |]

        let prefixAppPopups =
            prefixAppsData
            |> Seq.rev
            |> Seq.map (fun (prefixApp, _, _ as x) ->
                WorkflowPopupMenuOccurrence(RichText(normalizeDisplayText (prefixApp.GetText())), RichText.Empty, x, getPrefixAppRange))
            |> Array.ofSeq

        let selectedPrefixAppData = popupMenu.ShowPopup(textControl.Lifetime, prefixAppPopups, CustomHighlightingKind.Other, textControl, null)
        if isNull selectedPrefixAppData then () else
            
        let (prefixApp, argsCount, argExprs) = Seq.head (selectedPrefixAppData.Entities)

        let getPrefixAppWithArgsText (args: IFSharpExpression list) =
            normalizeDisplayText (String.Join(" ", prefixApp.GetText(), String.Join(" ", args |> List.map(fun x -> x.GetText()))))
        let getPrefixAppWithArgsRange args = [|getTreeNodesDocumentRange prefixApp (args |> List.last)|]

        let argExprsPopups =
            [1 .. argsCount]
            |> Seq.map (fun i -> argExprs |> List.take i)
            |> Seq.rev
            |> Seq.map (fun args -> WorkflowPopupMenuOccurrence(RichText(getPrefixAppWithArgsText args), RichText.Empty, args, getPrefixAppWithArgsRange))
            |> Array.ofSeq

        let argExprsToApply = popupMenu.ShowPopup(textControl.Lifetime, argExprsPopups, CustomHighlightingKind.Other, textControl, null)      
        if isNull argExprsToApply then () else

        prefixAppWithArgsToApply <- (prefixApp, Seq.head (argExprsToApply.Entities))
        base.Execute(solution, textControl)  

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(errorPrefixApp.IsPhysical())
        let factory = errorPrefixApp.CreateElementFactory()
        use disableFormatter = new DisableCodeFormatter()

        let (prefixAppToApply, argExprsToApply) = prefixAppWithArgsToApply
        let newPrefixAppTree = createPrefixAppExprTree factory prefixAppToApply argExprsToApply
        let updatedPrefixAppTree = ModificationUtil.ReplaceChild(prefixAppToApply, newPrefixAppTree)
        let updatedPrefixAppTreeWithParens = addParens updatedPrefixAppTree

        let parentPrefixApp = PrefixAppExprNavigator.GetByArgumentExpression(updatedPrefixAppTreeWithParens.IgnoreParentParens())
        ModificationUtil.ReplaceChild(getParentPrefixApp parentPrefixApp argExprsToApply.Length, parentPrefixApp) |> ignore
