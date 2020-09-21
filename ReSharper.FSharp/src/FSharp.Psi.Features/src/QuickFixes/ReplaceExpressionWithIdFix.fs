namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

type ReplaceExpressionWithIdFix(expr: IFSharpExpression) =
    inherit FSharpQuickFixBase()

    let hasIdCollision =
        match expr with
        | :? ILambdaExpr as lambda ->
            match lambda.CheckerService.ResolveNameAtLocation(lambda.RArrow, ["id"], "ReplaceExpressionWithIdFix") with
            | Some symbolUse -> symbolUse.Symbol.FullName <> "Microsoft.FSharp.Core.Operators.id"
            | None -> false
        | _ -> false

    override x.Text =
        match expr with
        | :? ILambdaExpr as lambda ->
            if lambda.PatternsEnumerable.CountIs(1) then "Replace lambda with 'id'" else "Replace lambda body with 'id'"
        | _ -> "Replace with 'id'"

    new(warning: ExpressionCanBeReplacedWithIdWarning) = ReplaceExpressionWithIdFix(warning.Expr)
    
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
            let arrow = lambda.RArrow
            let pats = lambda.Patterns
            let nodeAfterPats = lambda.Parameters.NextSibling
            let replaceBody = pats.Count = 1

            if replaceBody then
                let paren = ParenExprNavigator.GetByInnerExpression(lambda)
                let nodeToReplace = if isNotNull paren then paren :> IFSharpExpression else expr
                replace nodeToReplace (factory.CreateReferenceExpr("id"))
            else
                if nodeAfterPats != arrow then replaceRangeWithNode nodeAfterPats arrow.PrevSibling (Whitespace(1))
                else ModificationUtil.AddChildBefore(arrow, Whitespace(1)) |> ignore

                deleteChildRange (pats.[pats.Count - 2]).NextSibling (pats.Last())
                replace lambda.Expression (factory.CreateReferenceExpr("id"))
        | _ -> ()
