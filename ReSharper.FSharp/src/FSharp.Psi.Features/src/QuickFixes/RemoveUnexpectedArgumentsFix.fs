namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

type RemoveUnexpectedArgumentsFix(warning: NotAFunctionError) =
    inherit FSharpQuickFixBase()
        
    let notAFunctionExpr = warning.NotAFunctionExpr
    let unexpectedArgs = warning.UnexpectedArgs
    
    override x.Text = "Remove unexpected arguments"

    override x.IsAvailable _ =
        isValid notAFunctionExpr && not (notAFunctionExpr :? ILiteralExpr)

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(notAFunctionExpr.IsPhysical())     
        for arg in unexpectedArgs do
            let firstToDelete = getFirstMatchingNodeBefore (fun x -> x.IsWhitespaceToken()) arg
            let lastToDelete = getLastMatchingNodeAfter (fun x -> x.IsWhitespaceToken()) arg
            deleteChildRange firstToDelete lastToDelete
