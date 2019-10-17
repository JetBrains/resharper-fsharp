namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl

type RemoveUnusedLocalBindingFix(warning: UnusedValueWarning) =
    inherit QuickFixBase()

    let pat = warning.Pat.IgnoreParentParens()

    // todo: check there's a single declaration pat
    // todo: we can also check that every top declaration pat is unused instead

    let binding = LocalBindingNavigator.GetByHeadPattern(pat)
    let letOrUseExpr = LetOrUseExprNavigator.GetByBinding(binding)

    let getRanges (expr: ILetOrUseExpr) =
        Assertion.Assert(expr.Bindings.Count = 1, "expr.Bindings.Count = 1")

        let outerSeqExpr = SequentialExprNavigator.GetByExpression(expr)
        let inExpr = expr.InExpression

        if isNotNull outerSeqExpr && inExpr :? ISequentialExpr then
            TreeRange(expr), TreeRange(inExpr.FirstChild, inExpr.LastChild) else

        let inKeyword = expr.InKeyword
        if isNotNull inKeyword then
            let start = getRangeEndWithNewLineAfter inKeyword
            TreeRange(expr), TreeRange(start.NextSibling, inExpr) else

        let replaceRange =
            getRangeWithNewLineBefore expr
        
        let copyRange =
            let start = getRangeEndWithNewLineAfter expr.Bindings.[0]
            TreeRange(start.NextSibling, inExpr)

        replaceRange, copyRange

    override x.Text =
        match pat with
        | :? IParametersOwnerPat -> "Remove unused function"
        | _ -> "Remove unused value"

    override x.IsAvailable _ =
        isValid pat && isValid letOrUseExpr && isValid letOrUseExpr.InExpression

    override x.ExecutePsiTransaction(_, _) =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        let bindings = letOrUseExpr.Bindings
        if bindings.Count = 1 then
            let toReplace, toCopy = getRanges letOrUseExpr
            ModificationUtil.ReplaceChildRange(toReplace, toCopy) |> ignore
            null
        else
            let bindingIndex = bindings.IndexOf(binding)

            let rangeToDelete =
                if bindingIndex = 0 then
                    let andKeyword = letOrUseExpr.Separators.[0]
                    TreeRange(getRangeEndWithSpaceBefore binding, andKeyword)
                else
                    let andKeyword = letOrUseExpr.Separators.[bindingIndex - 1]
                    TreeRange(getRangeStartWithNewLineBefore andKeyword, getRangeEndWithSpaceAfter binding)

            ModificationUtil.DeleteChildRange(rangeToDelete)

            Action<_>(fun textControl ->
                let anchorBindingIndex = if bindingIndex > 0 then bindingIndex - 1 else 0
                let offset = letOrUseExpr.Bindings.[anchorBindingIndex].GetNavigationRange().EndOffset
                textControl.Caret.MoveTo(offset, CaretVisualPlacement.DontScrollIfVisible))
