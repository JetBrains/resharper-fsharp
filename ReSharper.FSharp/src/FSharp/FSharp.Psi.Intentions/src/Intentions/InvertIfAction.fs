namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<ContextAction(Name = "InvertIf", GroupType = typeof<FSharpContextActions>, Description = "Invert 'if' expression")>]
type InvertIfAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    override x.Text = "Invert 'if'"

    override x.IsAvailable _ =
        let ifExpr = dataProvider.GetSelectedElement<IIfThenElseExpr>()
        if isNull ifExpr then false else

        let thenExpr = ifExpr.ThenExpr
        let elseExpr = ifExpr.ElseExpr
        if isNull thenExpr || isNull elseExpr || elseExpr :? IElifExpr then false else

        if not (isAtIfExprKeyword dataProvider ifExpr) then false else

        if ifExpr.IsSingleLine then true else

        // Allow inverting simple expressions of form
        // if foo then "a" else
        //
        // "b"

        if not (thenExpr.IsSingleLine && elseExpr.IsSingleLine) then false else

        let beforeThenExpr = skipMatchingNodesBefore isWhitespace thenExpr
        let beforeElseExpr = skipMatchingNodesBefore isWhitespace elseExpr

        getTokenType beforeThenExpr == FSharpTokenType.THEN &&
        getTokenType beforeElseExpr == FSharpTokenType.ELSE

    override x.ExecutePsiTransaction(_, _) =
        let ifExpr = dataProvider.GetSelectedElement<IIfThenElseExpr>()
        use writeCookie = WriteLockCookie.Create(ifExpr.IsPhysical())

        let conditionExpr = ifExpr.ConditionExpr
        let negatedExpression = createLogicallyNegatedExpression conditionExpr
        let replaced = ModificationUtil.ReplaceChild(conditionExpr, negatedExpression)
        addParensIfNeeded replaced |> ignore

        let oldThenExpr = ifExpr.ThenExpr
        let thenExpr = oldThenExpr.Copy()
        let elseExpr = ifExpr.ElseExpr

        ModificationUtil.ReplaceChild(oldThenExpr, elseExpr.IgnoreInnerParens().Copy()) |> addParensIfNeeded |> ignore
        ModificationUtil.ReplaceChild(elseExpr, thenExpr.IgnoreInnerParens()) |> addParensIfNeeded |> ignore

        null
