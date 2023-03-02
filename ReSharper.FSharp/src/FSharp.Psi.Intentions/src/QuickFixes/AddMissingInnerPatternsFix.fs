namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Collections.Generic
open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.MatchTree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<AbstractClass>]
type AddMissingMatchClausesFixBase(warning: MatchIncompleteWarning) =
    inherit FSharpQuickFixBase()

    let matchExpr = warning.Expr.As<IMatchExpr>()

    abstract MarkAdditionalUsedNodes: value: MatchValue * Deconstructions * usedNodes: List<MatchNode> -> unit
    default this.MarkAdditionalUsedNodes(_, _, _) = ()

    abstract GetGenerationDeconstructions: value: MatchValue * Deconstructions -> Deconstructions
    default this.GetGenerationDeconstructions(_, existingDeconstructions) = existingDeconstructions

    override this.IsAvailable _ =
        isValid matchExpr &&

        // todo: fix parser match expr recovery when no clause is present
        let firstClause = matchExpr.ClausesEnumerable.FirstOrDefault()

        isNotNull firstClause &&
        isNotNull firstClause.Bar &&
        firstClause.Indent = matchExpr.Indent

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(matchExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        use pinCheckResultsCookie = matchExpr.FSharpFile.PinTypeCheckResults(true, this.Text)

        let value, nodes, deconstructions = MatchTree.ofMatchExpr matchExpr

        this.MarkAdditionalUsedNodes(value, deconstructions, nodes)

        let deconstructions = this.GetGenerationDeconstructions(value, deconstructions)
        MatchTree.generateClauses matchExpr value nodes deconstructions


type AddMissingPatternsFix(warning: MatchIncompleteWarning) =
    inherit AddMissingMatchClausesFixBase(warning)

    let matchExpr = warning.Expr.As<IMatchExpr>()

    override this.MarkAdditionalUsedNodes(value, deconstructions, usedNodes) =
        let matchPattern = MatchTest.initialPattern deconstructions matchExpr false value
        let node = MatchNode.Create(value, matchPattern)

        while MatchNode.incrementAndTryReject deconstructions usedNodes matchExpr node do
            ()

    override this.GetGenerationDeconstructions(value, _) =
        let deconstructions = OneToListMap()
        markToLevelDeconstructions deconstructions value
        deconstructions

    override this.Text = "Add missing patterns"


type AddMissingInnerPatternsFix(warning: MatchIncompleteWarning) =
    inherit AddMissingMatchClausesFixBase(warning)

    override this.IsAvailable _ =
        let productConfigurations = Shell.Instance.GetComponent<RunsProducts.ProductConfigurations>()
        productConfigurations.IsInternalMode()

    override this.Text = "Add missing inner patterns"
