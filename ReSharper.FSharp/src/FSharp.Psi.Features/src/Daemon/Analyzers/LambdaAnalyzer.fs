namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open System.Collections.Generic
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<ElementProblemAnalyzer(typeof<ILambdaExpr>,
                         HighlightingTypes = [|typeof<LambdaCanBeSimplifiedWarning>
                                               typeof<LambdaCanBeReplacedWarning>
                                               typeof<ExpressionCanBeReplacedWithIdWarning>|])>]
type LambdaAnalyzer() =
    inherit ElementProblemAnalyzer<ILambdaExpr>()

    let rec patIsUsed (usedNames: ISet<_>) (pat: IFSharpPattern)  =
        match pat.IgnoreInnerParens() with
        | :? ITuplePat as tuplePat -> Seq.exists (patIsUsed usedNames) tuplePat.PatternsEnumerable
        | :? ILocalReferencePat as refPat -> usedNames.Contains(refPat.SourceName)
        | _ -> false

    let rec compareArg (pat: IFSharpPattern) (arg: IFSharpExpression) =
        match pat.IgnoreInnerParens(), arg.IgnoreInnerParens() with
        | :? ITuplePat as pat, (:? ITupleExpr as expr) ->
            (isNull pat.StructKeyword = isNull expr.StructKeyword) && 
            compareArgsSeq pat.PatternsEnumerable expr.ExpressionsEnumerable
        | :? ILocalReferencePat as pat, (:? IReferenceExpr as reference) ->
            pat.SourceName = reference.ShortName
        | :? IUnitPat, (:? IUnitExpr) -> true
        | _ -> false

    and compareArgsSeq (pats: IFSharpPattern seq) (args: IFSharpExpression seq) =
        if args.IsEmpty() then pats.IsEmpty() else
        if pats.IsEmpty() then false else

        compareArg (Seq.head pats) (Seq.head args) && compareArgsSeq (Seq.tail pats) (Seq.tail args)

    and compareArgs (pats: TreeNodeCollection<_>) (expr: IFSharpExpression) =
        let rec compareArgsRec (expr: IFSharpExpression) i =
            let hasMatches = i > 0
            match expr.IgnoreInnerParens() with
            | :? IPrefixAppExpr as app when isNotNull app.ArgumentExpression && i <> pats.Count ->
                let pat = pats.[pats.Count - 1 - i]
                let equal = compareArg pat app.ArgumentExpression
                let funExpr = app.FunctionExpression

                let isPatRedundant =
                    equal &&
                    let usedNames = FSharpNamingService.getUsedNames funExpr EmptyList.Instance null false
                    not (patIsUsed usedNames pat)

                if isPatRedundant then compareArgsRec funExpr (i + 1) else (hasMatches, false, app :> IFSharpExpression)
            | x -> hasMatches, i = pats.Count, x

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

        if compareArg (pats.LastOrDefault()) expr then
            consumer.AddHighlighting(ExpressionCanBeReplacedWithIdWarning(lambda))
