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
open JetBrains.ReSharper.Plugins.FSharp.Psi.PsiUtil

type ReplaceWithTripleQuotedInterpolatedStringFix(error: SingleQuoteInSingleQuoteError) =
    inherit FSharpQuickFixBase()

    let getInterpolatedStringExpr (child: ITreeNode): IInterpolatedStringExpr =
        let mutable node = getParent child
        while node != null && not <| node :? IInterpolatedStringExpr do
            node <- getParent node
        node :?> _

    let makeTripleQuoted (node: ITreeNode): ITreeNode =
        let textWithBorders = node.GetText()

        match node.GetTokenType() with
        | tokenType when tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_START ||
                         tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING_START ->
            let startBorder = getStringStartingQuotesLength tokenType
            let stringContent = textWithBorders.Substring(startBorder, textWithBorders.Length - startBorder)
            FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_START.Create($"$\"\"\"{stringContent}")
        | tokenType when tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_END ||
                         tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING_END ->
            let endBorder = getStringEndingQuotesLength tokenType
            let stringContent = textWithBorders.Substring(0, textWithBorders.Length - endBorder)
            FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END.Create($"{stringContent}\"\"\"")
        | _ -> node

    override this.IsAvailable _ =
        if not <| isValid error.Expr then false else

        let parentExpr = getInterpolatedStringExpr error.Expr
        if isNull parentExpr then false else

        // Nested triple quoted interpolated strings represent not valid F# code, so ignore such possible case
        let grandparentExpr = getInterpolatedStringExpr parentExpr
        isNull grandparentExpr

    override this.Text = "Replace with triple-quoted interpolated string"

    override this.ExecutePsiTransaction (_: ISolution) =
        let expr = error.Expr
        let interpolatedExpr = getInterpolatedStringExpr expr

        if isNull interpolatedExpr then () else

        use _ = WriteLockCookie.Create()

        let firstChild = interpolatedExpr.FirstChild
        let lastChild = interpolatedExpr.LastChild

        let newFirstChild = makeTripleQuoted firstChild
        let newLastChild = makeTripleQuoted lastChild

        ModificationUtil.ReplaceChild(firstChild, newFirstChild) |> ignore
        ModificationUtil.ReplaceChild(lastChild, newLastChild) |> ignore
