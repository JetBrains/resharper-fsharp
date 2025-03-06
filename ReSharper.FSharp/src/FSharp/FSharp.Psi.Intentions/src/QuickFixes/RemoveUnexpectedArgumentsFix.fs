namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util

[<AutoOpen>]
module private FixNames =
    let [<Literal>] RemoveUnexpectedArgument = "Remove unexpected argument"
    let [<Literal>] RemoveUnexpectedArguments = "Remove unexpected arguments"

type RemoveUnexpectedArgumentsFix(warning: NotAFunctionError) =
    inherit FSharpQuickFixBase()

    let expr = warning.Expr
    let prefixApp = warning.PrefixApp

    override x.Text =
        if prefixApp.FunctionExpression == expr then RemoveUnexpectedArgument else RemoveUnexpectedArguments

    override x.IsAvailable _ = isValid prefixApp && isValid expr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())

        let firstUnexpectedArg = PrefixAppExprNavigator.GetByFunctionExpression(expr).ArgumentExpression
        let commentNodeCandidate = skipMatchingNodesBefore isWhitespace firstUnexpectedArg
        let updatedRoot = ModificationUtil.ReplaceChild(prefixApp, expr.Copy())

        if commentNodeCandidate != expr then
            addNodesAfter updatedRoot (TreeRange(expr.NextSibling, commentNodeCandidate)) |> ignore
