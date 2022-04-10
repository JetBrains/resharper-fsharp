namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.StringLiteralsUtil
open JetBrains.ReSharper.Resources.Shell

type ReplaceWithTripleQuotedInterpolatedStringFix(error: SingleQuoteInSingleQuoteError) =
    inherit FSharpQuickFixBase()

    let makeTripleQuoted (node: ITreeNode): ITreeNode =
        let textWithBorders = node.GetText()

        match node.GetTokenType() with
        | tokenType when tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_START ->
            let startBorder = getStringStartingQuotesLength tokenType
            let stringContent = textWithBorders.Substring(startBorder, textWithBorders.Length - startBorder)
            FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_START.Create($"$\"\"\"{stringContent}")
        | tokenType when tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_END ->
            let endBorder = getStringEndingQuotesLength tokenType
            let stringContent = textWithBorders.Substring(0, textWithBorders.Length - endBorder)
            FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END.Create($"{stringContent}\"\"\"")
        | _ -> node

    override this.IsAvailable _ =
        if not <| isValid error.Expr then false else

        let parentExpr = error.Expr.GetContainingNode<IInterpolatedStringExpr>()
        if isNull parentExpr then false else

        // Nested triple quoted interpolated strings represent not valid F# code, so ignore such possible case
        let grandparentExpr = parentExpr.GetContainingNode<IInterpolatedStringExpr>()
        if isNotNull grandparentExpr then false else

        // Executing this quickfix on verbatim strings will change literal semantics, so disabling it
        parentExpr.FirstChild.GetTokenType() != FSharpTokenType.VERBATIM_INTERPOLATED_STRING_START

    override this.Text = "Replace with triple-quoted interpolated string"

    override this.ExecutePsiTransaction (_: ISolution) =
        let interpolatedExpr = error.Expr.GetContainingNode<IInterpolatedStringExpr>()

        use _ = WriteLockCookie.Create()

        ModificationUtil.ReplaceChild(interpolatedExpr.FirstChild, makeTripleQuoted interpolatedExpr.FirstChild)
        |> ignore

        ModificationUtil.ReplaceChild(interpolatedExpr.LastChild, makeTripleQuoted interpolatedExpr.LastChild)
        |> ignore
