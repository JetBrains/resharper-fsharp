namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Collections.Generic
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.QuickFixes.MatchTree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

[<AbstractClass>]
type AddMissingMatchClausesFixBase(warning: MatchIncompleteWarning) =
    inherit FSharpQuickFixBase()

    let matchExpr = warning.Expr.As<IMatchExpr>()

    abstract MarkAdditionalUsedNodes:
        value: MatchValue * existingDeconstructions: Deconstructions * usedNodes: List<MatchNode> -> unit

    abstract GetGenerationDeconstructions:
        value: MatchValue * existingDeconstructions: Deconstructions -> Deconstructions

    override this.IsAvailable _ =
        isValid matchExpr &&

        // todo: fix parser match expr recovery when no clause is present
        matchExpr.ClausesEnumerable |> Seq.isEmpty |> not

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(matchExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        use pinCheckResultsCookie = matchExpr.FSharpFile.PinTypeCheckResults(true, this.Text)

        let factory = matchExpr.CreateElementFactory()
        let value, nodes, deconstructions = MatchTree.ofMatchExpr matchExpr

        this.MarkAdditionalUsedNodes(value, deconstructions, nodes)

        let tryAddClause (node: MatchNode) =
            if nodes |> Seq.exists (MatchTest.matches node) then
                () else

            let matchClause = 
                addNodesAfter matchExpr.LastChild [
                    NewLine(matchExpr.GetLineEnding())
                    let indent = matchExpr.Indent
                    if indent > 0 then
                        Whitespace(indent)
                    factory.CreateMatchClause()
                ] :?> IMatchClause

            let usedNames = HashSet()            
            MatchNode.bind usedNames matchClause.Pattern node

        let deconstructions = this.GetGenerationDeconstructions(value, deconstructions)

        let matchPattern = MatchTest.initialPattern deconstructions matchExpr value
        let node = MatchNode.Create(value, matchPattern)

        tryAddClause node
        while MatchNode.increment deconstructions matchExpr node node do
            tryAddClause node


type AddMissingPatternsFix(warning: MatchIncompleteWarning) =
    inherit AddMissingMatchClausesFixBase(warning)

    let matchExpr = warning.Expr.As<IMatchExpr>()

    let markToLevelDeconstructions (deconstructions: Deconstructions) (value: MatchValue) =
        deconstructions[value.Path] <- Deconstruction.InnerPatterns

        match value.Type with
        | MatchType.Tuple(_, matchTypes) ->
            matchTypes
            |> Array.iteri (fun i _ ->
                let itemPath = MatchTest.TupleItem i :: value.Path
                deconstructions[itemPath] <- Deconstruction.InnerPatterns
            )

        | _ ->
            ()

    override this.MarkAdditionalUsedNodes(value, deconstructions, usedNodes) =
        let matchPattern = MatchTest.initialPattern deconstructions matchExpr value
        let node = MatchNode.Create(value, matchPattern)

        while MatchNode.incrementAndTryReject deconstructions usedNodes matchExpr node node do
            ()

    override this.GetGenerationDeconstructions(value, _) =
        let deconstructions = Dictionary()
        markToLevelDeconstructions deconstructions value
        deconstructions

    override this.Text = "Add missing patterns"


type AddMissingInnerPatternsFix(warning: MatchIncompleteWarning) =
    inherit AddMissingMatchClausesFixBase(warning)

    override this.Text = "Add missing inner patterns"

    override this.MarkAdditionalUsedNodes(_, _, _) =
        ()

    override this.GetGenerationDeconstructions(_, existingDeconstructions) =
        existingDeconstructions
