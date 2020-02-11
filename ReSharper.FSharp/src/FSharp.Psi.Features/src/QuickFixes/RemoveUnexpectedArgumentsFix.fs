namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

type RemoveUnexpectedArgumentsFix(warning: NotAFunctionError) =
    inherit FSharpQuickFixBase()
    
    let removeUnexpectedArgumentsWithWhitespaces (prefixApp: IPrefixAppExpr) =
        let firstToDelete = getFirstMatchingNodeBefore (fun x -> x.IsWhitespaceToken()) prefixApp.LastChild
        let lastToDelete = getLastMatchingNodeAfter (fun x -> x.IsWhitespaceToken()) prefixApp.LastChild
        deleteChildRange firstToDelete lastToDelete
        
    let notAFunctionExpr = warning.NotAFunctionExpr
    
    override x.Text = "Remove unexpected arguments"

    override x.IsAvailable _ =
        isValid notAFunctionExpr && not (notAFunctionExpr :? ILiteralExpr)

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(notAFunctionExpr.IsPhysical())     
        inspectUnexpectedArgs removeUnexpectedArgumentsWithWhitespaces notAFunctionExpr
