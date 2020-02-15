namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl

[<AutoOpen>]
module private FixNames =
    let [<Literal>] RemoveUnexpectedArgument = "Remove unexpected argument"
    let [<Literal>] RemoveUnexpectedArguments = "Remove unexpected arguments"

type RemoveUnexpectedArgumentsFix(warning: NotAFunctionError) =
    inherit FSharpQuickFixBase()

    let expr = warning.Expr
    let prefixApp = warning.PrefixApp
    
    override x.Text =
        if prefixApp.FunctionExpression == expr.IgnoreParentParens() then RemoveUnexpectedArgument else RemoveUnexpectedArguments

    override x.IsAvailable _ = isValid prefixApp && isValid expr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        replace prefixApp (expr.IgnoreParentParens())
