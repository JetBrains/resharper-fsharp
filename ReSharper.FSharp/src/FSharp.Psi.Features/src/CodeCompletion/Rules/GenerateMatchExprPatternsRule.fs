namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.UI.RichText
open JetBrains.Util

module GenerateMatchExprPatternsInfo =
    let [<Literal>] Id = "Match values"


type GenerateMatchExprPatternsInfo() =
    inherit TextualInfo("_ -> ()", GenerateMatchExprPatternsInfo.Id)

    override this.IsRiderAsync = false


type GenerateMatchExprPatternsBehavior(info) =
    inherit TextualBehavior<GenerateMatchExprPatternsInfo>(info)

    override this.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill)

        let psiServices = solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()

        use pinCheckResultsCookie =
            textControl.GetFSharpFile(solution).PinTypeCheckResults(true, GenerateMatchExprPatternsInfo.Id)

        let node = textControl.Document.GetPsiSourceFile(solution).FSharpFile.FindNodeAt(nameRange)
        let matchExpr = node.GetContainingNode<IMatchExpr>()

        do
            use writeCookie = WriteLockCookie.Create(matchExpr.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()
            use transactionCookie =
                PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices,
                    GenerateMatchExprPatternsInfo.Id)

            let deleteRangeStart = skipMatchingNodesAfter isInlineSpaceOrComment matchExpr.WithKeyword
            ModificationUtil.DeleteChildRange(deleteRangeStart, matchExpr.Clauses[0])

            let value, nodes, _ = MatchTree.ofMatchExpr matchExpr

            let deconstructions = OneToListMap()
            MatchTree.markToLevelDeconstructions deconstructions value

            MatchTree.generateClauses matchExpr value nodes deconstructions

        let range = matchExpr.Clauses[0].Expression.GetNavigationRange()
        textControl.Caret.MoveTo(range.EndOffset, CaretVisualPlacement.DontScrollIfVisible)
        textControl.Selection.SetRange(range)


[<Language(typeof<FSharpLanguage>)>]
type GenerateMatchExprPatternsRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let [<Literal>] Precedence =
        CLRLookupItemRelevance.ExpectedTypeMatch |||
        CLRLookupItemRelevance.ExpectedTypeMatchLambda

    let getMatchExpr (context: FSharpCodeCompletionContext) =
        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        if isNull reference then null else

        let referenceName = reference.GetElement().As<IExpressionReferenceName>()
        let refPat = LocalReferencePatNavigator.GetByReferenceName(referenceName)
        let matchClause = MatchClauseNavigator.GetByPattern(refPat)
        MatchExprNavigator.GetByClause(matchClause)

    override this.IsAvailable(context) =
        not context.IsQualified &&

        let matchExpr = getMatchExpr context
        isNotNull matchExpr && matchExpr.Clauses.Count = 1

    override this.AddLookupItems(context, collector) =
        let info = GenerateMatchExprPatternsInfo(Ranges = context.Ranges)
        let item =
            LookupItemFactory.CreateLookupItem(info)
                .WithPresentation(fun _ -> TextualPresentation(RichText("Match values"), info) :> _)
                .WithBehavior(fun _ -> GenerateMatchExprPatternsBehavior(info) :> _)
                .WithMatcher(fun _ -> TextualMatcher("Match values", info) :> _)
                .WithRelevance(Precedence)

        item.Placement.Location <- PlacementLocation.Top
        collector.Add(item)

        false
