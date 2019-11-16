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

    let binding = BindingNavigator.GetByHeadPattern(pat)
    let letOrUse = LetNavigator.GetByBinding(binding)

    let getRanges (expr: ILetLikeExpr) =
        let outerSeqExpr = SequentialExprNavigator.GetByExpression(expr)
        let inExpr = expr.InExpression

        if isNotNull outerSeqExpr && inExpr :? ISequentialExpr then
            TreeRange(expr), TreeRange(inExpr.FirstChild, inExpr.LastChild) else

        let inKeyword = expr.InKeyword
        if isNotNull inKeyword then
            let start = getRangeEndWithNewLineAfter inKeyword
            TreeRange(expr), TreeRange(start.NextSibling, inExpr) else

        let replaceRange =
            match CompExprNavigator.GetByExpression(expr) with
            | null -> getRangeWithNewLineBefore expr
            | _ -> TreeRange(expr)
        
        let copyRange =
            let start = getRangeEndWithNewLineAfter expr.Bindings.[0]
            TreeRange(start.NextSibling, inExpr)

        replaceRange, copyRange

    override x.Text =
        match pat with
        | :? IParametersOwnerPat -> "Remove unused function"
        | _ -> "Remove unused value"

    override x.IsAvailable _ =
        isValid pat && isValid letOrUse

    override x.ExecutePsiTransaction(_, _) =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())

        let bindings = letOrUse.Bindings
        if bindings.Count = 1 then
            match letOrUse with
            | :? ILetModuleDecl as letModuleDecl ->
                let first = getRangeStartWithNewLineBefore letModuleDecl
                let last = getRangeEndWithSpaceAfter letModuleDecl
                ModificationUtil.DeleteChildRange(TreeRange(first, last))

            | :? ILetLikeExpr as letExpr ->
                let toReplace, toCopy = getRanges letExpr
                ModificationUtil.ReplaceChildRange(toReplace, toCopy) |> ignore

            | _ ->
                failwithf "Unexpected let: %O" letOrUse
            null
        else
            let letBindings = letOrUse.As<ILetBindings>().NotNull()
            let bindingIndex = bindings.IndexOf(binding)

            let rangeToDelete =
                if bindingIndex = 0 then
                    let andKeyword = letBindings.Separators.[0]
                    TreeRange(getRangeStartWithSpaceBefore binding, andKeyword)
                else
                    let andKeyword = letBindings.Separators.[bindingIndex - 1]
                    TreeRange(getRangeStartWithNewLineBefore andKeyword, getRangeEndWithSpaceAfter binding)

            ModificationUtil.DeleteChildRange(rangeToDelete)

            Action<_>(fun textControl ->
                let anchorIndex = if bindingIndex > 0 then bindingIndex - 1 else 0
                let offset = letBindings.Bindings.[anchorIndex].GetNavigationRange().EndOffset
                textControl.Caret.MoveTo(offset, CaretVisualPlacement.DontScrollIfVisible))
