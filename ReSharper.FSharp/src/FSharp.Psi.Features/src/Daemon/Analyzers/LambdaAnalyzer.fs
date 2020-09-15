namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<ElementProblemAnalyzer(typeof<ILambdaExpr>,
                         HighlightingTypes = [|typeof<LambdaCanBeSimplifiedWarning>;
                                               typeof<LambdaCanBeReplacedWarning>|])>]
type LambdaAnalyzer() =
    inherit ElementProblemAnalyzer<ILambdaExpr>()

    let rec compareArg (pat: IFSharpPattern) (arg: IFSharpExpression) =
        match pat.IgnoreInnerParens(), arg.IgnoreInnerParens() with
        | :? ITuplePat as pat, (:? ITupleExpr as expr) ->
            compareArgsSeq pat.PatternsEnumerable expr.ExpressionsEnumerable
        | :? ILocalReferencePat as pat, (:? IReferenceExpr as reference) ->
            pat.SourceName = reference.ShortName
        | :? IUnitPat, (:? IUnitExpr) -> true
        | _ -> false

    and compareArgsSeq (pats: IFSharpPattern seq) (args: IFSharpExpression seq) =
        if args.IsEmpty() then pats.IsEmpty() else
        if pats.IsEmpty() then false else

        compareArg (Seq.head pats) (Seq.head args) && compareArgsSeq (Seq.tail pats) (Seq.tail args)

    and compareArgs (pats: TreeNodeCollection<IFSharpPattern>) (expr: IFSharpExpression) =
        let rec compareArgsRec (expr: IFSharpExpression) i =
            let hasMatches = i > 0

            match expr with
            | :? IPrefixAppExpr as app when isNotNull app.ArgumentExpression && i <> pats.Count ->
                let equal = compareArg pats.[pats.Count - 1 - i] app.ArgumentExpression
                let app = app.FunctionExpression.IgnoreInnerParens()

                if equal then compareArgsRec app (i + 1) else (hasMatches, false, app)
            | _ -> hasMatches, i = pats.Count, expr

        compareArgsRec expr 0

    let isApplicable (expr: IFSharpExpression) =
        match expr with
        | :? IPrefixAppExpr
        | :? IReferenceExpr
        | :? ITupleExpr
        | :? IUnitExpr -> true
        | _ -> false

    override x.Run(lambda, _, consumer) =
        let expr = lambda.Expression.IgnoreInnerParens()
        if not (isApplicable expr) then () else

        let pats = lambda.Patterns

        match compareArgs pats expr with
        | true, true, replaceCandidate ->
            consumer.AddHighlighting(LambdaCanBeReplacedWarning(lambda, replaceCandidate))
        | true, false, replaceCandidate ->
            consumer.AddHighlighting(LambdaCanBeSimplifiedWarning(lambda, replaceCandidate))
        | _ ->

        match compareArg (pats.LastOrDefault()) expr, pats.Count = 1 with
        | true, true -> consumer.AddHighlighting(LambdaCanBeReplacedWarning(lambda, null))
        | true, false -> consumer.AddHighlighting(LambdaCanBeSimplifiedWarning(lambda, null))
        | _ -> ()
