namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpResolveUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type ReplaceLambdaWithBuiltinFunctionFix(warning: LambdaCanBeReplacedWithBuiltinFunctionWarning) =
    inherit FSharpQuickFixBase()

    let lambda = warning.Lambda
    let exprToReplace = lambda.IgnoreParentParens()
    let funName = warning.FunName

    override x.IsAvailable _ =
        isValid exprToReplace &&
        resolvesToPredefinedFunction lambda.RArrow funName "ReplaceLambdaWithBuiltinFunctionFix"

    override x.Text = sprintf "Replace lambda with '%s'" funName

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(exprToReplace.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        let factory = exprToReplace.CreateElementFactory()
        
        let prevToken = exprToReplace.GetPreviousToken()
        let nextToken = exprToReplace.GetNextToken()

        if isNotNull prevToken && not (isWhitespace prevToken) then addNodeBefore exprToReplace (Whitespace())
        if isNotNull nextToken && not (isWhitespace nextToken) then addNodeAfter exprToReplace (Whitespace())

        replace exprToReplace (factory.CreateReferenceExpr(funName))
