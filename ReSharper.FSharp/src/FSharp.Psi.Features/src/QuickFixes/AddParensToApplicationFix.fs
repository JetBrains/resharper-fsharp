namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open System.Text.RegularExpressions
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypesUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ProjectModel
open JetBrains.UI.RichText
open JetBrains.Util

module AddParensToApplicationFix =
    let [<Literal>] AppPopupName = "AppPopup"
    let [<Literal>] ArgsPopupName = "ArgsPopup"

type AddParensToApplicationFix(error: NotAFunctionError) =
    inherit FSharpQuickFixBase()

    let [<Literal>] popupItemMaxLength = 80

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

    let rec createAppExpr (factory: IFSharpElementFactory) (expr: IFSharpExpression) args =
        match args with
        | head :: tail -> createAppExpr factory (factory.CreateAppExpr(expr, head, true)) tail
        | [] -> expr

    let getMaxArgsCount (expr: IFSharpExpression) =
        let fcsType = expr.TryGetFcsType()

        if isNotNull fcsType && fcsType.IsFunctionType then
            List.length (getFunctionTypeArgs fcsType) - 1

        else
            match expr.IgnoreInnerParens() with
            | :? ILambdaExpr as lambda ->
                lambda.PatternsEnumerable.Count()

            | :? IReferenceExpr as ref ->
                match ref.Reference.GetFSharpSymbol() with
                | :? FSharpMemberOrFunctionOrValue as mfv when
                    let parameters = mfv.CurriedParameterGroups
                    parameters.Count > 0 && parameters.[0].Count > 0 ->
                        match mfv.FullTypeSafe with
                        | Some t -> List.length (getFunctionTypeArgs t) - 1
                        | None -> 0
                | _ -> 0
            | _ -> 0

    let findAppsWithoutParens prefixAppExpr =
        let rec collectAppliedExprsRec (prefixAppExpr: IPrefixAppExpr) prefixAppDataAcc appliedExprsAcc =
            let maxArgsCount = getMaxArgsCount prefixAppExpr.ArgumentExpression

            let isApplicableApp =
                maxArgsCount > 0 && not (List.isEmpty appliedExprsAcc)

            let appDataAcc =
                if isApplicableApp then
                    {| App = prefixAppExpr.ArgumentExpression
                       MaxArgsCount = maxArgsCount
                       ArgCandidates = appliedExprsAcc |} :: prefixAppDataAcc
                else prefixAppDataAcc

            match prefixAppExpr.FunctionExpression.IgnoreInnerParens() with
            | :? IPrefixAppExpr as appExpr ->
                collectAppliedExprsRec appExpr appDataAcc (prefixAppExpr.ArgumentExpression :: appliedExprsAcc)
            | _ -> appDataAcc

        collectAppliedExprsRec prefixAppExpr [] []

    let appCandidates = findAppsWithoutParens errorPrefixApp

    override x.Text =
        match appCandidates with
        | [appData] ->
            match appData.App.IgnoreInnerParens() with
            | :? IReferenceExpr as refExpr when refExpr.ShortName <> SharedImplUtil.MISSING_DECLARATION_NAME -> 
                $"Add parens to '{refExpr.ShortName}' application"

            | :? ILambdaExpr -> "Add parens to lambda application"
            | _ -> "Add parens to application"

        | _ -> "Add parens to application"

    override x.IsAvailable _ =
        match appCandidates with
        | [] -> false
        | list -> list |> List.forall (fun appData -> isValid appData.App)

    override x.Execute(solution, textControl) =
        let popupMenu = solution.GetComponent<WorkflowPopupMenu>()

        let showPopup occurrences id =
            popupMenu.ShowPopup(textControl.Lifetime, occurrences, CustomHighlightingKind.Other, textControl, null, id)

        let appOccurrences =
            appCandidates
            |> Seq.rev
            |> Seq.map (fun x ->
                WorkflowPopupMenuOccurrence(
                    RichText(toDisplay (x.App.GetText())), RichText.Empty, x,
                    (fun appData -> [| appData.App.GetNavigationRange() |])))
            |> Array.ofSeq

        let appOccurrence = showPopup appOccurrences AddParensToApplicationFix.AppPopupName

        if isNull appOccurrence then () else       

        let appData = Seq.head appOccurrence.Entities

        let argOccurrences =
            [ 1 .. Math.Min(appData.MaxArgsCount, appData.ArgCandidates.Length) ]
            |> Seq.rev
            |> Seq.map (fun i ->
                let args = appData.ArgCandidates |> List.take i
                let argsTexts = args |> List.map (fun x -> x.GetText()) |> String.concat " "
                WorkflowPopupMenuOccurrence(
                    RichText(toDisplay(String.Join(" ", appData.App.GetText(), argsTexts))), RichText.Empty, args,
                    (fun args -> [| getTreeNodesDocumentRange appData.App (args |> List.last) |])))
            |> Array.ofSeq

        let argsOccurrence = showPopup argOccurrences AddParensToApplicationFix.ArgsPopupName
        if isNull argsOccurrence then () else

        appToApply <- appData.App
        argsToApply <- Seq.head argsOccurrence.Entities
        base.Execute(solution, textControl)

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(errorPrefixApp.IsPhysical())
        use disableFormatterCookie = new DisableCodeFormatter()
        let factory = errorPrefixApp.CreateElementFactory()

        let newAppExpr = createAppExpr factory appToApply argsToApply
        let newAppExpr = ModificationUtil.ReplaceChild(appToApply, newAppExpr)

        let parenExpr = ParenExprNavigator.GetByInnerExpression(addParens newAppExpr)
        let parentApp = PrefixAppExprNavigator.GetByArgumentExpression(parenExpr)

        ModificationUtil.ReplaceChild(getParentPrefixApp parentApp argsToApply.Length, parentApp) |> ignore
