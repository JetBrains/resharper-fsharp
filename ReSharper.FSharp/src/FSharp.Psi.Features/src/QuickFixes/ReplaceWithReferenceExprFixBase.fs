namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<AbstractClass>]
type ReplaceWithReferenceExprFixBase(expr: IFSharpExpression, refExprName, refExprFullName) =
    inherit FSharpQuickFixBase()
    
    abstract member ResolveContext: ITreeNode
    abstract member AdditionalExecute: unit -> unit
    default x.AdditionalExecute() = ()
    
    member x.ExprToReplace = expr

    override x.IsAvailable _ =
        isValid expr &&
        match expr.CheckerService.ResolveNameAtLocation(x.ResolveContext, [refExprName], "ReplaceWithReferenceExprFixBase") with
        | Some symbolUse -> symbolUse.Symbol.FullName = refExprFullName
        | None -> true

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        let factory = expr.CreateElementFactory()

        x.AdditionalExecute()
        replace expr (factory.CreateReferenceExpr(refExprName))
