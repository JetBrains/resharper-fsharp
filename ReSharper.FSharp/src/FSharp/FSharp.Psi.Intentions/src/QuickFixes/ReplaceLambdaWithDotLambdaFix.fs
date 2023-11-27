namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ReplaceLambdaWithDotLambdaFix(warning: DotLambdaCanBeUsedWarning) =
    inherit FSharpScopedQuickFixBase(warning.Lambda)

    let lambda = warning.Lambda
    let expr = warning.Lambda.Expression.IgnoreInnerParens()

    override x.IsAvailable _ = isValid lambda && isValid expr && isContextWithoutWildPats expr

    override x.Text = "Replace with shorthand lambda"

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        let factory = expr.CreateElementFactory()

        let firstToDelete = warning.RootRefExpr.IgnoreParentParens()
        let delimiter = QualifiedExprNavigator.GetByQualifier(firstToDelete).Delimiter
        let lastToDelete = getLastMatchingNodeAfter isInlineSpaceOrComment delimiter
        deleteChildRange firstToDelete lastToDelete

        let dotLambda = factory.CreateDotLambda()
        dotLambda.SetExpression(expr) |> ignore

        let exprToReplace = lambda.IgnoreParentParens()
        let addSpace =
            match exprToReplace.PrevSibling with
            | null -> false
            | x when x.IsFiltered() -> false
            | _ -> true
        if addSpace then addNodeBefore exprToReplace (Whitespace(1))
        replace exprToReplace dotLambda
