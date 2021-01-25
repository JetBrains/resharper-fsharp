namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl

type RemoveUnusedLocalBindingFix(warning: UnusedValueWarning) =
    inherit FSharpQuickFixBase()

    let pat = warning.Pat.IgnoreParentParens()

    // todo: check there's a single declaration pat
    // todo: we can also check that every top declaration pat is unused instead

    let binding = BindingNavigator.GetByHeadPattern(pat)
    let letOrUse = LetBindingsNavigator.GetByBinding(binding)

    let getCopyRange (expr: ILetOrUseExpr) =
        let inExpr = expr.InExpression

        let inKeyword = expr.InKeyword
        if isNotNull inKeyword then
            let start =
                inKeyword
                |> skipMatchingNodesAfter isInlineSpaceOrComment
                |> skipNewLineAfter

            TreeRange(start, inExpr) else

        let first =
            binding
            |> skipMatchingNodesAfter isInlineSpaceOrComment
            |> getThisOrNextNewLine
            |> skipMatchingNodesAfter isInlineSpaceOrComment

        TreeRange(first, inExpr)

    override x.Text =
        let binding = BindingNavigator.GetByHeadPattern(pat)
        if isNotNull binding && binding.HasParameters then
            "Remove unused function"
        else
            "Remove unused value"

    override x.IsAvailable _ =
        isValid pat && isValid letOrUse &&

        not (pat :? IAsPat) // todo: enable, check inner patterns

    override x.ExecutePsiTransaction(_, _) =
        use writeLock = WriteLockCookie.Create(pat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let bindings = letOrUse.Bindings
        if bindings.Count = 1 then
            match letOrUse with
            | :? ILetBindingsDeclaration as letBindings ->
                let first =
                    letBindings
                    |> skipMatchingNodesBefore isInlineSpaceOrComment
                    |> getThisOrPrevNewLIne

                let last =
                    letBindings
                    |> getLastMatchingNodeAfter isInlineSpaceOrComment

                ModificationUtil.DeleteChildRange(TreeRange(first, last))

            | :? ILetOrUseExpr as letExpr ->
                let rangeToCopy = getCopyRange letExpr
                ModificationUtil.ReplaceChildRange(TreeRange(letExpr), rangeToCopy) |> ignore

            | _ ->
                failwithf "Unexpected let: %O" letOrUse
            null
        else
            let letBindings = letOrUse.As<ILetBindings>().NotNull()
            let bindingIndex = bindings.IndexOf(binding)

            let rangeToDelete =
                if bindingIndex = 0 then
                    let andKeyword = letBindings.Separators.[0]
                    TreeRange(getFirstMatchingNodeBefore isInlineSpaceOrComment binding, andKeyword)
                else
                    let andKeyword = letBindings.Separators.[bindingIndex - 1]
                    let rangeStart = getFirstMatchingNodeBefore isInlineSpaceOrComment andKeyword

                    let rangeEnd =
                        binding
                        |> skipMatchingNodesAfter isInlineSpaceOrComment
                        |> getThisOrNextNewLine

                    TreeRange(rangeStart, rangeEnd)

            ModificationUtil.DeleteChildRange(rangeToDelete)

            Action<_>(fun textControl ->
                let anchorIndex = if bindingIndex > 0 then bindingIndex - 1 else 0
                let offset = letBindings.Bindings.[anchorIndex].GetNavigationRange().EndOffset
                textControl.Caret.MoveTo(offset, CaretVisualPlacement.DontScrollIfVisible))
