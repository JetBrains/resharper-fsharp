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
        let app = lambda.Expression.IgnoreInnerParens() :?> IPrefixAppExpr
        let pats = lambda.Patterns

        let rec compareArgsRec (app: IPrefixAppExpr) i =
            let hasMatches = not (i = 0)
            if isNull app || isNull app.ArgumentExpression || i = pats.Count then (hasMatches, app) else

            let equal = compareArg pats.[pats.Count - 1 - i] app.ArgumentExpression
            let app = if isNull app then null else app.FunctionExpression.As<IPrefixAppExpr>()

            if equal then compareArgsRec app (i + 1) else (hasMatches, app)

        compareArgsRec (app: IPrefixAppExpr) 0

    override x.Run(lambda, _, consumer) =
        if isNull lambda.Expression then () else
 
        let (canBeSimplified, appForReplace) = 
            match lambda.Expression.IgnoreInnerParens() with
            | :? IPrefixAppExpr ->  compareArgs lambda
            | x when (x :? IReferenceExpr || x :? ITupleExpr || x :? IUnitExpr) ->
                compareArg (lambda.PatternsEnumerable.LastOrDefault()) x, null
            | _ -> false, null

        if canBeSimplified then
            consumer.AddHighlighting(LambdaCanBeSimplifiedWarning(lambda, appForReplace), lambda.GetHighlightingRange())
