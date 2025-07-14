namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpParensUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type ReplaceLambdaWithDotLambdaFix(warning: DotLambdaCanBeUsedWarning) =
    inherit FSharpScopedQuickFixBase(warning.Lambda)

    let lambda = warning.Lambda
    let expr = warning.Lambda.Expression.IgnoreInnerParens()

    override x.IsAvailable _ =
       isValid lambda && isValid expr &&
       (FSharpLanguageLevel.isFSharp81Supported expr || isContextWithoutWildPats expr)

    override x.Text = "Replace with shorthand lambda"

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())

        let factory = expr.CreateElementFactory()

        let firstToDelete = warning.RootRefExpr.IgnoreParentParens()
        let delimiter = QualifiedExprNavigator.GetByQualifier(firstToDelete).Delimiter
        let lastToDelete = getLastMatchingNodeAfter isInlineSpaceOrComment delimiter
        deleteChildRange firstToDelete lastToDelete

        let dotLambda = factory.CreateDotLambda()
        dotLambda.SetExpression(expr) |> ignore

        let exprToReplace: ITreeNode =
            let possibleParenExpr = lambda.IgnoreParentParens(includingBeginEndExpr = false)
            if needsParens possibleParenExpr dotLambda then lambda else possibleParenExpr

        ModificationUtil.ReplaceChild(exprToReplace, dotLambda) |> ignore
