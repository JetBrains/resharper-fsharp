namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Feature.Services.QuickFixes.Scoped
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type IFSharpRemoveRedundantParensFix =
    inherit IQuickFix

type FSharpRemoveRedundantParensScopedFixingStrategy() =
    inherit ScopedFixingStrategyBase()

    static member val Instance: IScopedFixingStrategy = FSharpRemoveRedundantParensScopedFixingStrategy() :> _

    override this.IsEnabled(analyzer: IElementProblemAnalyzer): bool =
        analyzer :? IFSharpRedundantParenAnalyzer

    override this.IsEnabled(quickFix: IQuickFix): bool =
        quickFix :? IFSharpRemoveRedundantParensFix


[<AbstractClass>]
type FSharpRemoveRedundantParensFixBase(parensNode: IFSharpTreeNode, innerNode: IFSharpTreeNode) =
    inherit FSharpScopedQuickFixBase()

    override x.Text = "Remove redundant parens"

    override x.IsAvailable _ =
        isValid parensNode && isValid innerNode

    override this.TryGetContextTreeNode() = parensNode :> _

    override this.GetScopedFixingStrategy _ =
        FSharpRemoveRedundantParensScopedFixingStrategy.Instance

    abstract AddSpaceAfter: prevToken: ITokenNode -> bool
    default _.AddSpaceAfter(prevToken: ITokenNode) = isIdentifierOrKeyword prevToken

    abstract AddSpaceBefore: nextToken: ITokenNode -> bool
    default _.AddSpaceBefore(nextToken: ITokenNode) = isIdentifierOrKeyword nextToken

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(parensNode.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let parenExprIndent = parensNode.Indent
        let innerExprIndent = innerNode.Indent
        let indentDiff = parenExprIndent - innerExprIndent

        if x.AddSpaceAfter(parensNode.GetPreviousToken()) then
            ModificationUtil.AddChildBefore(parensNode, Whitespace()) |> ignore

        if x.AddSpaceBefore(parensNode.GetNextToken()) then
            ModificationUtil.AddChildAfter(parensNode, Whitespace()) |> ignore

        let expr = ModificationUtil.ReplaceChild(parensNode, innerNode.Copy())
        shiftNode indentDiff expr

    interface IFSharpRemoveRedundantParensFix


type RemoveRedundantParenExprFix(warning: RedundantParenExprWarning) =
    inherit FSharpRemoveRedundantParensFixBase(warning.ParenExpr, warning.ParenExpr.InnerExpression)

    override this.IsAvailable(var0) =
        base.IsAvailable(var0) &&

        let innerExpr = warning.ParenExpr.InnerExpression
        let context = innerExpr.IgnoreParentParens()

        not (needsParens context innerExpr)

type RemoveRedundantParenTypeUsageFix(warning: RedundantParenTypeUsageWarning) =
    inherit FSharpRemoveRedundantParensFixBase(warning.ParenTypeUsage, warning.ParenTypeUsage.InnerTypeUsage)

    override this.IsAvailable(data) =
        base.IsAvailable(data) &&

        let innerExpr = warning.ParenTypeUsage.InnerTypeUsage
        let context = innerExpr.IgnoreParentParens()

        not (RedundantParenTypeUsageAnalyzer.needsParens context innerExpr)

    override this.AddSpaceAfter(prevToken) =
        getTokenType prevToken != FSharpTokenType.LESS && base.AddSpaceAfter(prevToken)

    override this.AddSpaceBefore(nextToken) =
        getTokenType nextToken != FSharpTokenType.GREATER && base.AddSpaceAfter(nextToken)

type RemoveRedundantParenPatFix(warning: RedundantParenPatWarning) =
    inherit FSharpRemoveRedundantParensFixBase(warning.ParenPat, warning.ParenPat.Pattern)

    override this.IsAvailable(data) =
        base.IsAvailable(data) &&

        let innerExpr = warning.ParenPat.Pattern
        let context = innerExpr.IgnoreParentParens()

        not (RedundantParenPatAnalyzer.needsParens context innerExpr)
