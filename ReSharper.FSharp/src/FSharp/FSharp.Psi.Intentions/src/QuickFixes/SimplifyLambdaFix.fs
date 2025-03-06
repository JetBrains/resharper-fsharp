namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpLambdaUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type SimplifyLambdaFix(warning: LambdaCanBeSimplifiedWarning) =
    inherit FSharpQuickFixBase()

    let lambda = warning.LambdaExpr
    let replaceCandidate = warning.ReplaceCandidate

    let countRedundantArgs expr =
        let rec countRedundantArgsRec (expr: IFSharpExpression) i =
            let expr = PrefixAppExprNavigator.GetByFunctionExpression(expr.IgnoreParentParens())
            if isNotNull expr then countRedundantArgsRec expr (i + 1) else i

        countRedundantArgsRec expr 0

    override x.Text = "Simplify lambda"

    override x.IsAvailable _ = isValid lambda && isValid replaceCandidate

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(lambda.IsPhysical())

        ModificationUtil.ReplaceChild(lambda.Expression, replaceCandidate) |> ignore
        let redundantArgsCount = countRedundantArgs replaceCandidate
        deletePatternsFromEnd lambda redundantArgsCount
