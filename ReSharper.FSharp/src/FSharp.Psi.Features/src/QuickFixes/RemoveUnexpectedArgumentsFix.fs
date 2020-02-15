namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

[<AutoOpen>]
module private FixNames =
    let [<Literal>] RemoveUnexpectedArgument = "Remove unexpected argument"
    let [<Literal>] RemoveUnexpectedArguments = "Remove unexpected arguments"

type RemoveUnexpectedArgumentsFix(warning: NotAFunctionError) =
    inherit FSharpQuickFixBase()

    let expr = warning.NotAFunctionExpr
    let prefixApp = warning.PrefixAppExpr
    
    override x.Text =
        if warning.UnexpectedArgs.Length > 1 then RemoveUnexpectedArguments else RemoveUnexpectedArgument 

    override x.IsAvailable _ = isValid prefixApp && isValid expr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        let replaceExpr =
            match ParenExprNavigator.GetByInnerExpression(expr) with
            | null -> expr
            | x -> x :> _
        replace prefixApp replaceExpr
