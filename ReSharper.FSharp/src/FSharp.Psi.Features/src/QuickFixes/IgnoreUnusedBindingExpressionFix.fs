namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type IgnoreUnusedBindingExpressionFix(warning: UnusedValueWarning) =
    inherit FSharpQuickFixBase()

    let pat = warning.Pat.IgnoreParentParens()
    let binding = BindingNavigator.GetByHeadPattern(pat)
    let letOrUseExpr = LetOrUseExprNavigator.GetByBinding(binding)

    let rec getCorrectAnchor (expr: ITreeNode): ITreeNode =
        match expr with
        | :? ISequentialExpr as seqExpr when not (seqExpr.ExpressionsEnumerable.IsEmpty()) ->
            let last = seqExpr.ExpressionsEnumerable.LastOrDefault()
            if last :? ILetOrUseExpr then getCorrectAnchor last else last :> _

        | :? ILetOrUseExpr as letExpr when isNotNull letExpr.InExpression -> getCorrectAnchor letExpr.InExpression
        | _ ->

        let exprCopy = expr.Copy()
        let seqExpr = ModificationUtil.ReplaceChild(expr, ElementType.SEQUENTIAL_EXPR.Create())
        ModificationUtil.AddChild(seqExpr, exprCopy)

    override x.Text = "Ignore expression"

    override x.IsAvailable _ =
        isValid pat && not (pat :? IParametersOwnerPat) && isValid letOrUseExpr && letOrUseExpr.Bindings.Count = 1 &&
        isValid binding.Expression && not (binding.Expression :? IDoExpr)

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        use formatter = FSharpRegistryUtil.AllowFormatterCookie.Create()

        if not (binding.Expression.Type().IsVoid()) then
            ignoreInnermostExpression binding.Expression false

        let inExpr = letOrUseExpr.InExpression
        let newLine = NewLine(letOrUseExpr.GetLineEnding())

        let bindingExpr = ModificationUtil.ReplaceChild(letOrUseExpr, binding.Expression)
        addNodesAfter (getCorrectAnchor bindingExpr) [
            newLine
            newLine
            inExpr
        ] |> ignore
