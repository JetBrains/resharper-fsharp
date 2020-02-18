namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.Tree

[<AutoOpen>]
module private FixNames =
    let [<Literal>] RemoveUnexpectedArgument = "Remove unexpected argument"
    let [<Literal>] RemoveUnexpectedArguments = "Remove unexpected arguments"

type RemoveUnexpectedArgumentsFix(warning: NotAFunctionError) =
    inherit FSharpQuickFixBase()

    let expr = warning.Expr
    let prefixApp = warning.PrefixApp

    let isFromCommentTokensRegion (x: ITreeNode) =
        match x with
        | x when x.IsWhitespaceToken() -> true
        | x when x.IsCommentToken() -> true
        | x when x.IsNewLineToken() -> true
        | _ -> false
    
    override x.Text =
        if prefixApp.FunctionExpression == expr then RemoveUnexpectedArgument else RemoveUnexpectedArguments

    override x.IsAvailable _ = isValid prefixApp && isValid expr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        let commentTokensRegion = getAllMatchingNodesAfter isFromCommentTokensRegion expr
        let updatedRoot = replaceWithCopy prefixApp expr
        addNodesAfter updatedRoot commentTokensRegion |> ignore