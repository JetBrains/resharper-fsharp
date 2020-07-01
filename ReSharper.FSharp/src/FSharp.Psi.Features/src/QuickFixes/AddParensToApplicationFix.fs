namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
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
        
    let tryFindPrefixAppWithoutParens prefixAppExpr =
        let rec collectAppliedExprsRec (prefixAppExpr : IPrefixAppExpr) (appliedExprsAcc: _ list) =
            let functionExpression = prefixAppExpr.FunctionExpression.IgnoreInnerParens()
            match prefixAppExpr.ArgumentExpression with
            | :? IReferenceExpr as refExpr->
                match refExpr.Reference.GetFSharpSymbol() with
                | :? FSharpMemberOrFunctionOrValue as memberOrFunctionOrValue ->
                    let parametersCount = memberOrFunctionOrValue.CurriedParameterGroups.Count
                    if memberOrFunctionOrValue.FullType.IsFunctionType &&
                       parametersCount <= appliedExprsAcc.Length then
                        (Some (refExpr), appliedExprsAcc |> List.take parametersCount)
                    else (None, [])
                | _ ->
                    match functionExpression with
                    | :? IPrefixAppExpr as appExpr -> collectAppliedExprsRec appExpr (prefixAppExpr.ArgumentExpression :: appliedExprsAcc)
                    | _ -> (None, [])
            | _ ->
                match functionExpression with
                | :? IPrefixAppExpr as appExpr -> collectAppliedExprsRec appExpr (prefixAppExpr.ArgumentExpression :: appliedExprsAcc)
                | _ -> (None, [])

        collectAppliedExprsRec prefixAppExpr []

    do
        let (x, y) = tryFindPrefixAppWithoutParens errorPrefixApp
        prefixAppToApply <- x
        argExprsToApply <- y

    override x.Text =
        match prefixAppToApply with
        | Some prefixApp -> sprintf "Add parens to '%s' application" "foo"
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
