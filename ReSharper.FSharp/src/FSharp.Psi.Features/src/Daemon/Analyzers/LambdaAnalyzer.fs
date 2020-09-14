namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.Util
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Psi.Tree

[<ElementProblemAnalyzer(typeof<ILambdaExpr>, HighlightingTypes = [|typeof<LambdaCanBeSimplifiedWarning>|])>]
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

        let equal = compareArg (Seq.head pats) (Seq.head args)
        if equal then compareArgsSeq (Seq.tail pats) (Seq.tail args) else false

    and compareArgs (lambda: ILambdaExpr) =
        let expr = lambda.Expression.IgnoreInnerParens()
        let pats = lambda.Patterns

        let rec compareArgsRec (expr: IFSharpExpression) i =
            let hasMatches = not (i = 0)
            match expr with
            | :? IPrefixAppExpr as app when isNotNull app.ArgumentExpression && not (i = pats.Count) ->
                let equal = compareArg pats.[pats.Count - 1 - i] app.ArgumentExpression
                let app = if isNull app then null else app.FunctionExpression

                if equal then compareArgsRec app (i + 1) else (hasMatches, app)
            | _ -> hasMatches, expr

        compareArgsRec expr 0

    override x.Run(lambda, _, consumer) =
        if isNull lambda.Expression then () else
 
        let (canBeSimplified, exprForReplace) = 
            match lambda.Expression.IgnoreInnerParens() with
            | :? IPrefixAppExpr ->  compareArgs lambda
            | x when (x :? IReferenceExpr || x :? ITupleExpr || x :? IUnitExpr) ->
                compareArg (lambda.PatternsEnumerable.LastOrDefault()) x, null
            | _ -> false, null

        if canBeSimplified then
            consumer.AddHighlighting(LambdaCanBeSimplifiedWarning(lambda, exprForReplace), lambda.GetHighlightingRange())
