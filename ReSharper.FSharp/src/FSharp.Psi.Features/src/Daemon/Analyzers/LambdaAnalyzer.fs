namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.Util
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors

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

    let addLambdaCanBeSimplifiedWarning (consumer: IHighlightingConsumer) (lambda: ILambdaExpr) (exprForReplace: IFSharpExpression) =
        consumer.AddHighlighting(LambdaCanBeSimplifiedWarning(lambda, exprForReplace), lambda.GetHighlightingRange())

    let addLambdaCanBeReplacedWarning (consumer: IHighlightingConsumer) (lambda: ILambdaExpr) (replaceCandidate: IFSharpExpression) =
        consumer.AddHighlighting(LambdaCanBeReplacedWarning(lambda, replaceCandidate), lambda.GetHighlightingRange())
    
    override x.Run(lambda, _, consumer) =
        if isNull lambda.Expression then () else
 
        match lambda.Expression.IgnoreInnerParens() with
        | :? IPrefixAppExpr ->
            let (hasArgsMatches, exprForReplace) = compareArgs lambda

            if hasArgsMatches then
                match exprForReplace with
                | x when (isNotNull x && x.IgnoreInnerParens() :? IPrefixAppExpr) ->
                    addLambdaCanBeSimplifiedWarning consumer lambda x
                | x ->
                    addLambdaCanBeReplacedWarning consumer lambda x

        | x when (x :? IReferenceExpr || x :? ITupleExpr || x :? IUnitExpr) ->
            let hasMatch = compareArg (lambda.PatternsEnumerable.LastOrDefault()) x

            if hasMatch then 
                if lambda.PatternsEnumerable.CountIs 1 then
                    addLambdaCanBeReplacedWarning consumer lambda null
                else
                    addLambdaCanBeSimplifiedWarning consumer lambda null
        | _ -> ()
