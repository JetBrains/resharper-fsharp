namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

type ReplaceExpressionWithIdFix(expr: IFSharpExpression) =
    inherit FSharpQuickFixBase()

    let hasIdCollision =
        match expr with
        | :? ILambdaExpr as lambda ->
            match lambda.CheckerService.ResolveNameAtLocation(lambda.RArrow, ["id"], "ReplaceExpressionWithIdFix") with
            | Some symbolUse -> symbolUse.Symbol.FullName <> "Microsoft.FSharp.Core.Operators.Id"
            | None -> false
        | _ -> false

    override x.Text =
        match expr with
        | :? ILambdaExpr as lambda ->
            if lambda.PatternsEnumerable.CountIs(1) then "Replace lambda body 'id'" else "Replace lambda with 'id'"
        | _ -> "Replace with 'id'"

    new(warning: LambdaCanBeSimplifiedWarning) = ReplaceExpressionWithIdFix(warning.LambdaExpr)
    new(warning: LambdaCanBeReplacedWarning) = ReplaceExpressionWithIdFix(warning.LambdaExpr)
    
    override x.IsAvailable _ =
        let isApplicable =
            match expr with
            | :? ILambdaExpr as lambda -> isValid lambda
            | _ -> false
         
        isApplicable && not hasIdCollision

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        let factory = expr.CreateElementFactory()

        match expr with
        | :? ILambdaExpr as lambda ->
            let pats = lambda.PatternsEnumerable
            if not (pats.CountIs(1)) then
                replaceRangeWithNode (pats.LastOrDefault()) lambda.RArrow.PrevSibling (Whitespace(1))
                replace lambda.Expression (factory.CreateReferenceExpr("id"))
            else
                let paren = ParenExprNavigator.GetByInnerExpression(lambda)
                let nodeToReplace = if isNotNull paren then paren :> IFSharpExpression else expr
                replace nodeToReplace (factory.CreateReferenceExpr("id"))
        | _ -> ()
