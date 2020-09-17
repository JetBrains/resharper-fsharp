namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open System.Collections.Generic
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings
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

    let rec containsAnyNode (set: ISet<string>) (nodes: ITreeNode seq) =
        if nodes.IsEmpty() then false else

        let contains = 
            match Seq.head nodes with
            | :? ILocalReferencePat as pat -> set.Contains(pat.SourceName)
            | _ -> false

        if contains then true else containsAnyNode set (Seq.tail nodes)
    
    let rec collectPatsPatterns (pats: IFSharpPattern seq) (acc: IList<_>) =
        if pats.IsEmpty() then () else 

        collectPatPatterns (Seq.head pats) acc
        collectPatsPatterns (Seq.tail pats) acc
    
    and collectPatPatterns (pat: IFSharpPattern) (acc: IList<_>) =
        match pat.IgnoreInnerParens() with
        | :? ITuplePat as pat -> collectPatsPatterns pat.PatternsEnumerable acc
        | :? ILocalReferencePat as pat -> acc.Add(pat :> ITreeNode)
        | _ -> ()

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
            match expr.IgnoreInnerParens() with
            | :? IPrefixAppExpr as app when isNotNull app.ArgumentExpression && i <> pats.Count ->
                let pat = pats.[pats.Count - 1 - i]
                let equal = compareArg pat app.ArgumentExpression
                let funExpr = app.FunctionExpression

                let continueCompare =
                    equal &&
                    let pats = List()
                    collectPatPatterns pat (List())     
                    let usedNames = FSharpNamingService.getUsedNames funExpr pats null
                    not (containsAnyNode usedNames pats)

                if continueCompare then compareArgsRec funExpr (i + 1) else (hasMatches, false, app :> IFSharpExpression)
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

        match compareArg (pats.LastOrDefault()) expr, pats.Count = 1 with
        | true, true -> consumer.AddHighlighting(LambdaCanBeReplacedWarning(lambda, null))
        | true, false -> consumer.AddHighlighting(LambdaCanBeSimplifiedWarning(lambda, null))
        | _ -> ()
