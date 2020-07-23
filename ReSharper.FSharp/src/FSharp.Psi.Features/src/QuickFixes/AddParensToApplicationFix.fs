namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Text.RegularExpressions
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

    let popupItemMaxLength = 80
    let errorPrefixApp = error.PrefixApp
    let mutable appToApply = null
    let mutable argsToApply = []

    let toDisplay (text: string) =
        if text.Length <= popupItemMaxLength then text else
        let text = Regex("\n\s*", RegexOptions.Compiled).Replace(text, " ↩ ")
        let diff = text.Length - popupItemMaxLength
        let center = text.Length / 2
        let textBefore = text.Substring(0, center - diff / 2)
        let textAfter = text.Substring(center + diff / 2)
        String.Concat(textBefore, " ... ", textAfter)

    let getParentPrefixApp expr nestingLevel =
        let rec getParentPrefixAppRec expr i =
            let parentPrefixApp = PrefixAppExprNavigator.GetByFunctionExpression(expr)
            if i + 1 <= nestingLevel
            then getParentPrefixAppRec parentPrefixApp (i + 1)
            else parentPrefixApp

        getParentPrefixAppRec expr 1

    let rec createAppExprTree (factory: IFSharpElementFactory) (expr: IFSharpExpression) args =
        match args with
        | head :: tail -> createAppExprTree factory (factory.CreateAppExpr(expr, head, true)) tail
        | [] -> expr

    let countArgs fsharpType =
        let rec countArgsRec (fsharpType: FSharpType) count =
            let functionCandidate = fsharpType.GenericArguments.[1]
            if functionCandidate.IsFunctionType then countArgsRec functionCandidate count + 1 else count

        countArgsRec fsharpType 1

    let findAppsWithoutParens prefixAppExpr =
        let rec collectAppliedExprsRec
                (prefixAppExpr: IPrefixAppExpr)
                (prefixAppDataAcc: _ list)
                (appliedExprsAcc: _ list) =

            let argExprFcsType = prefixAppExpr.ArgumentExpression.TryGetFSharpType()

            let maxArgsCount =
                if argExprFcsType != null && argExprFcsType.IsFunctionType
                then Some(countArgs argExprFcsType) else None

            let isPrefixAppWithoutParens =
                match maxArgsCount with
                | Some _ -> appliedExprsAcc.Length > 0
                | _ -> false

            let prefixAppDataAcc =
                if isPrefixAppWithoutParens then
                    {| App = prefixAppExpr.ArgumentExpression
                       MaxArgsCount = maxArgsCount.Value
                       ArgCandidates = appliedExprsAcc |} :: prefixAppDataAcc
                else prefixAppDataAcc

            match prefixAppExpr.FunctionExpression.IgnoreInnerParens() with
            | :? IPrefixAppExpr as appExpr ->
                collectAppliedExprsRec appExpr prefixAppDataAcc (prefixAppExpr.ArgumentExpression :: appliedExprsAcc)
            | _ -> prefixAppDataAcc

        collectAppliedExprsRec prefixAppExpr [] []

    let appCandidates = findAppsWithoutParens errorPrefixApp

    override x.Text = "Add parens to application"

    override x.IsAvailable _ =
        match appCandidates with
        | [] -> false
        | list -> list |> List.forall (fun appData -> isValid appData.App)

    override x.Execute(solution, textControl) =
        let popupMenu = solution.GetComponent<WorkflowPopupMenu>()

        let appOccurrences =
            appCandidates
            |> Seq.rev
            |> Seq.map (fun x -> WorkflowPopupMenuOccurrence(
                                     RichText(toDisplay (x.App.GetText())),
                                     RichText.Empty, x,
                                     (fun appData -> [| appData.App.GetNavigationRange() |])))
            |> Array.ofSeq

        let appOccurrence =
            popupMenu.ShowPopup(textControl.Lifetime, appOccurrences, CustomHighlightingKind.Other, textControl, null)

        if isNull appOccurrence then () else       
        let appData = Seq.head (appOccurrence.Entities)

        let argOccurrences =
            [ 1 .. Math.Min(appData.MaxArgsCount, appData.ArgCandidates.Length) ]
            |> Seq.map (fun i -> appData.ArgCandidates |> List.take i)
            |> Seq.rev
            |> Seq.map (fun args -> WorkflowPopupMenuOccurrence(
                                        RichText(toDisplay(String.Join(" ", appData.App.GetText(), String.Join(" ", args |> List.map (fun x -> x.GetText()))))),
                                        RichText.Empty, args,
                                        (fun args -> [| getTreeNodesDocumentRange appData.App (args |> List.last) |])))
            |> Array.ofSeq

        let argsOccurrence =
            popupMenu.ShowPopup(textControl.Lifetime, argOccurrences, CustomHighlightingKind.Other, textControl, null)

        if isNull argsOccurrence then () else
        appToApply <- appData.App
        argsToApply <- Seq.head (argsOccurrence.Entities)
        base.Execute(solution, textControl)

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(errorPrefixApp.IsPhysical())
        let factory = errorPrefixApp.CreateElementFactory()
        use disableFormatter = new DisableCodeFormatter()

        let newAppExpr = createAppExprTree factory appToApply argsToApply
        let newAppExpr = ModificationUtil.ReplaceChild(appToApply, newAppExpr)
        let newAppExpr = addParens newAppExpr

        let parentApp = PrefixAppExprNavigator.GetByArgumentExpression(newAppExpr.IgnoreParentParens())
        ModificationUtil.ReplaceChild(getParentPrefixApp parentApp argsToApply.Length, parentApp) |> ignore
