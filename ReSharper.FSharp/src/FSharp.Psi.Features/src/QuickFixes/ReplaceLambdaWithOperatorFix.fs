namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpLambdaUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type ReplaceLambdaWithOperatorFix(warning: LambdaCanBeReplacedWithOperatorWarning) =
    inherit FSharpQuickFixBase()

    let lambda = warning.Lambda
    let op = warning.OpName
    let opFullName = sprintf "Microsoft.FSharp.Core.Operators.%s" op

    let opIsShadowed =
        match lambda.CheckerService.ResolveNameAtLocation(lambda.RArrow, [op], "ReplaceExpressionWithOperatorFix") with
        | Some symbolUse -> symbolUse.Symbol.FullName <> opFullName
        | None -> false

    override x.Text =
        if replaceLambdaBodyWithId lambda op then "Replace lambda body with 'id'"
        else sprintf "Replace lambda with '%s'" op

    override x.IsAvailable _ = isValid lambda && not opIsShadowed

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(lambda.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        let factory = lambda.CreateElementFactory()

        let replaceLambda = not (replaceLambdaBodyWithId lambda op)

        if replaceLambda then
            let paren = ParenExprNavigator.GetByInnerExpression(lambda)
            let nodeToReplace = if isNotNull paren then paren :> IFSharpExpression else lambda :> _

            let prevToken = nodeToReplace.GetPreviousToken()
            let nextToken = nodeToReplace.GetNextToken()

            if isNotNull prevToken && not (isWhitespace prevToken) then addNodeBefore nodeToReplace (Whitespace())
            if isNotNull nextToken && not (isWhitespace nextToken) then addNodeAfter nodeToReplace (Whitespace())

            replace nodeToReplace (factory.CreateReferenceExpr(op))
        else
            deletePatternsFromEnd lambda 1
            replace lambda.Expression (factory.CreateReferenceExpr(op))
