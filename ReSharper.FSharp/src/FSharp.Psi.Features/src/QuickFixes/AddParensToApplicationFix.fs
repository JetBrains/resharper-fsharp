namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl

type AddParensToApplicationFix(error: NotAFunctionError) =
    inherit FSharpQuickFixBase()

    let errorPrefixApp = error.PrefixApp
    let mutable prefixAppToApply = None
    let mutable argExprsToApply = []

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

    let tryFindPrefixAppWithoutParens prefixAppExpr =
        let rec collectAppliedExprsRec (prefixAppExpr : IPrefixAppExpr) (appliedExprsAcc: _ list) =
            let argExprFcsType = prefixAppExpr.ArgumentExpression.IgnoreInnerParens().TryGetFSharpType()
            if argExprFcsType != null && argExprFcsType.IsFunctionType then
                let expectedArgsCount = countArgs argExprFcsType
                if expectedArgsCount <= appliedExprsAcc.Length then
                    (Some (prefixAppExpr.ArgumentExpression), appliedExprsAcc |> List.take expectedArgsCount)
                else (None, [])         
            else match prefixAppExpr.FunctionExpression.IgnoreInnerParens() with
                 | :? IPrefixAppExpr as appExpr -> collectAppliedExprsRec appExpr (prefixAppExpr.ArgumentExpression :: appliedExprsAcc)
                 | _ -> (None, [])

        collectAppliedExprsRec prefixAppExpr []

    do let (x, y) = tryFindPrefixAppWithoutParens errorPrefixApp
       prefixAppToApply <- x
       argExprsToApply <- y

    override x.Text =
        match prefixAppToApply with
        | Some prefixApp ->
            let reference =
                match prefixApp.IgnoreInnerParens() with
                | :? IPrefixAppExpr as appExpr -> appExpr.Reference
                | :? IReferenceExpr as refExpr -> refExpr.Reference
                | _ -> null
            if isNotNull reference then sprintf "Add parens to '%s' application" (reference.GetName())
            else "Add parens to lambda application"
        | None -> ""

    override x.IsAvailable _ =
        match prefixAppToApply with
        | Some _ -> isValid errorPrefixApp
        | None -> false

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(errorPrefixApp.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let factory = errorPrefixApp.CreateElementFactory()
        let prefixAppToApply = match prefixAppToApply with | Some x -> x | None -> null
        let newPrefixAppTree = createPrefixAppExprTree factory prefixAppToApply argExprsToApply
        let updatedPrefixAppTree = ModificationUtil.ReplaceChild(prefixAppToApply, newPrefixAppTree)
        let updatedPrefixAppTreeWithParens = addParens updatedPrefixAppTree
        let parentPrefixApp = PrefixAppExprNavigator.GetByArgumentExpression(updatedPrefixAppTreeWithParens.IgnoreParentParens())
        ModificationUtil.ReplaceChild(getParentPrefixApp parentPrefixApp argExprsToApply.Length, parentPrefixApp) |> ignore
