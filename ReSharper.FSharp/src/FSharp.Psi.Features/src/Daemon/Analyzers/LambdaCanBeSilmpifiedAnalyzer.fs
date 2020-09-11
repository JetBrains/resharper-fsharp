namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.Util
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors

[<ElementProblemAnalyzer(typeof<ILambdaExpr>, HighlightingTypes = [|typeof<LambdaCanBeSimplifiedWarning>|])>]
type LambdaCanBeSimplifiedAnalyzer() =
    inherit ElementProblemAnalyzer<ILambdaExpr>()

    let rec compareArg (pat: IFSharpPattern) (arg: IFSharpExpression) =
        match pat.IgnoreInnerParens(), arg.IgnoreInnerParens() with
        | :? ITuplePat as pat, (:? ITupleExpr as tuple) -> compareArgsSeq pat.PatternsEnumerable tuple.ExpressionsEnumerable
        | :? ILocalReferencePat as pat, (:? IReferenceExpr as reference) -> pat.SourceName = reference.ShortName
        | :? IUnitPat, (:? IUnitExpr) -> true
        | _ -> false

    and compareArgsSeq (pats: IFSharpPattern seq) (args: IFSharpExpression seq) =
        if args.IsEmpty() then pats.IsEmpty() else
        if pats.IsEmpty() then false else

        let equal = compareArg (Seq.head pats) (Seq.head args)
        if equal then compareArgsSeq (Seq.tail pats) (Seq.tail args) else false

    and compareArgs (pats: IFSharpPattern seq) (app: IPrefixAppExpr) =
        let rec compareArgsRec (pats: IFSharpPattern seq) (app: IPrefixAppExpr) i =
            if isNull app || isNull app.ArgumentExpression || pats.IsEmpty() then (true, i) else

            let equal = compareArg (Seq.head pats) app.ArgumentExpression
            let app = if isNull app then null else app.FunctionExpression.As<IPrefixAppExpr>()

            if equal then compareArgsRec (Seq.tail pats) app (i + 1) else (not (i = 0) , i)

        compareArgsRec (pats: IFSharpPattern seq) (app: IPrefixAppExpr) 0

    override x.Run(lambda, _, consumer) =
        if isNull lambda.Expression then () else

        let pats = lambda.Patterns
 
        let (needWarning, redundantArgsCount) = 
            match lambda.Expression.IgnoreInnerParens() with
            | :? IPrefixAppExpr as app ->  compareArgs (Seq.rev pats) app
            | x when (x :? IReferenceExpr || x :? ITupleExpr || x :? IUnitExpr) -> compareArg (pats.Last()) x, 1
            | _ -> false, 0

        if needWarning then
            consumer.AddHighlighting(LambdaCanBeSimplifiedWarning(lambda, redundantArgsCount), lambda.GetHighlightingRange())
