namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type IgnoreUnusedBindingExpressionFix(warning: UnusedValueWarning) =
    inherit FSharpQuickFixBase()

    let pat = warning.Pat.IgnoreParentParens()
    let binding = BindingNavigator.GetByHeadPattern(pat)
    let letOrUseExpr = LetOrUseExprNavigator.GetByBinding(binding)
    
    let ignoreInnermostExpression (expr: IFSharpExpression) =
        let rec getInnermostExpression (expr: IFSharpExpression) =
            match expr with
            | :? ISequentialExpr as seqExpr -> getInnermostExpression (seqExpr.Expressions.Last())
            | :? ILetOrUseExpr as letOrUseExpr -> getInnermostExpression letOrUseExpr.InExpression
            | :? IParenExpr as parenExpr -> getInnermostExpression parenExpr.InnerExpression
            | _ -> expr
        
        let exprToIgnore = getInnermostExpression expr
        let ignoredExpr = exprToIgnore.CreateElementFactory().CreateIgnoreApp(exprToIgnore, false)
        let replaced = ModificationUtil.ReplaceChild(exprToIgnore, ignoredExpr)
        addParensIfNeeded replaced.LeftArgument |> ignore
    
    override x.Text = "Inline and ignore expression"

    override x.IsAvailable _ =
        isValid pat && isValid letOrUseExpr && letOrUseExpr.Bindings.Count = 1 && isValid binding.Expression
     && not (binding.Expression :? IDoExpr)

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        use formatter = FSharpRegistryUtil.AllowFormatterCookie.Create()
        
        if not (binding.Expression.Type().IsVoid()) then
            ignoreInnermostExpression binding.Expression
        
        let inExpr = letOrUseExpr.InExpression
        let newLine = NewLine(letOrUseExpr.GetLineEnding())
        
        let bindingExpr = ModificationUtil.ReplaceChild(letOrUseExpr, binding.Expression)
        addNodesAfter bindingExpr [
            newLine
            newLine
            inExpr
        ] |> ignore
