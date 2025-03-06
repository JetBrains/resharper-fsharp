namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Feature.Services.QuickFixes.Scoped
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type IFSharpRemoveRedundantParensFix =
    inherit IQuickFix

type FSharpRemoveRedundantParensScopedFixingStrategy() =
    inherit ScopedFixingStrategyBase()

    static let highlightingTypes =
        [| typeof<RedundantParenExprWarning>
           typeof<RedundantParenPatWarning>
           typeof<RedundantParenTypeUsageWarning> |]

    static member val Instance: IScopedFixingStrategy =
        FSharpRemoveRedundantParensScopedFixingStrategy() :> _

    override this.IsEnabled(highlighting: IHighlighting) =
        let highlightingType = highlighting.GetType()
        Array.contains highlightingType highlightingTypes

    override this.IsEnabled(quickFix: IQuickFix) =
        quickFix :? IFSharpRemoveRedundantParensFix

    override this.GetHighlightingTypes() =
        highlightingTypes :> _

    override this.GetQuickFixTypes() =
        [| typeof<IFSharpRemoveRedundantParensFix> |]

[<AbstractClass>]
type FSharpRemoveRedundantParensFixBase(parensNode: IFSharpTreeNode, innerNode: IFSharpTreeNode) =
    inherit FSharpScopedQuickFixBase(parensNode)

    override x.Text = "Remove redundant parens"

    override x.IsAvailable _ =
        isValid parensNode && isValid innerNode

    override this.GetScopedFixingStrategy(_, _) =
        FSharpRemoveRedundantParensScopedFixingStrategy.Instance

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(parensNode.IsPhysical())
        ModificationUtil.ReplaceChild(parensNode, innerNode.Copy()) |> ignore

    interface IFSharpRemoveRedundantParensFix


type RemoveRedundantParenExprFix(warning: RedundantParenExprWarning) =
    inherit FSharpRemoveRedundantParensFixBase(warning.ParenExpr, warning.ParenExpr.InnerExpression)

    override this.IsAvailable(var0) =
        base.IsAvailable(var0) &&

        let innerExpr = warning.ParenExpr.InnerExpression
        let context = innerExpr.IgnoreParentParens(includingBeginEndExpr = false)

        not (needsParens context innerExpr)

type RemoveRedundantParenTypeUsageFix(warning: RedundantParenTypeUsageWarning) =
    inherit FSharpRemoveRedundantParensFixBase(warning.ParenTypeUsage, warning.ParenTypeUsage.InnerTypeUsage)

    override this.IsAvailable(data) =
        base.IsAvailable(data) &&

        let innerExpr = warning.ParenTypeUsage.InnerTypeUsage
        let context = innerExpr.IgnoreParentParens()

        not (RedundantParenTypeUsageAnalyzer.needsParens context innerExpr)

type RemoveRedundantParenPatFix(warning: RedundantParenPatWarning) =
    inherit FSharpRemoveRedundantParensFixBase(warning.ParenPat, warning.ParenPat.Pattern)

    override this.IsAvailable(data) =
        base.IsAvailable(data) &&

        let innerExpr = warning.ParenPat.Pattern
        let context = innerExpr.IgnoreParentParens()

        not (ParenPatUtil.needsParens context innerExpr)
