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

type AddMissingMatchClausesFix(warning: MatchIncompleteWarning) =
    inherit FSharpQuickFixBase()

    let matchExpr = warning.Expr.As<IMatchExpr>()

    override this.IsAvailable _ =
        isValid matchExpr &&

        // todo: fix parser match expr recovery when no clause is present
        matchExpr.ClausesEnumerable |> Seq.isEmpty |> not

    override this.Text = "Add missing patterns"

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(matchExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        use pinCheckResultsCookie = matchExpr.FSharpFile.PinTypeCheckResults(true, this.Text)

        let factory = matchExpr.CreateElementFactory()
        let matchValue, nodes, deconstructions = MatchTree.ofMatchExpr matchExpr

        let tryAddClause (node: MatchNode) =
            if nodes |> Seq.exists (MatchTest.matches node) then () else

            let matchClause = 
                addNodesAfter matchExpr.LastChild [
                    NewLine(matchExpr.GetLineEnding())
                    Whitespace(matchExpr.Indent)
                    factory.CreateMatchClause()
                ] :?> IMatchClause

            let usedNames = HashSet()            
            MatchNode.bind usedNames matchClause.Pattern node

        let matchPattern = MatchTest.initialPattern deconstructions matchExpr matchValue
        let node = MatchNode.Create(matchValue, matchPattern)

        tryAddClause node
        while MatchNode.increment deconstructions matchExpr node do
            tryAddClause node
