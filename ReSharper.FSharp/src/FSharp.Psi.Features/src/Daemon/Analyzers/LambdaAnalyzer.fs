namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open FSharp.Compiler
open System
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<ElementProblemAnalyzer(typeof<ILambdaExpr>,
                         HighlightingTypes = [| typeof<LambdaCanBeSimplifiedWarning>
                                                typeof<LambdaCanBeReplacedWithInnerExpressionWarning>
                                                typeof<LambdaBodyCanBeReplacedWithIdWarning>
                                                typeof<LambdaCanBeReplacedWithBuiltinFunctionWarning>|])>]
type LambdaAnalyzer() =
    inherit ElementProblemAnalyzer<ILambdaExpr>()

    let rec patIsUsed (nameUsages: OneToListMap<string, ITreeNode>) (excludedUseExpr: ITreeNode) (pat: IFSharpPattern) =
        match pat.IgnoreInnerParens() with
        | :? ITuplePat as tuplePat -> Seq.exists (patIsUsed nameUsages excludedUseExpr) tuplePat.PatternsEnumerable
        | :? ILocalReferencePat as refPat ->
            nameUsages.GetValuesSafe(refPat.SourceName)
            |> Seq.exists (fun u -> isNotNull u && not (excludedUseExpr.Contains(u))) 
        | _ -> false

    let rec compareArg (pat: IFSharpPattern) (arg: IFSharpExpression) =
        match pat.IgnoreInnerParens(), arg.IgnoreInnerParens() with
        | :? ITuplePat as pat, (:? ITupleExpr as expr) ->
            // todo: remove with FCS update, fix tuple pattern ranges
            Shell.Instance.IsTestShell &&

            isNull pat.StructKeyword = isNull expr.StructKeyword && 
            compareArgsSeq pat.PatternsEnumerable expr.ExpressionsEnumerable

        | :? ILocalReferencePat as pat, (:? IReferenceExpr as expr) ->
            let patReferenceName = pat.ReferenceName
            if patReferenceName.IsQualified || expr.IsQualified then false else

            let patName = patReferenceName.ShortName
            if patName.IsEmpty() || not (Char.IsLower(patName.[0])) then false else

            patName = expr.ShortName

        | :? IUnitPat, (:? IUnitExpr) -> true
        | _ -> false

    and compareArgsSeq (pats: IFSharpPattern seq) (args: IFSharpExpression seq) =
        if args.IsEmpty() then pats.IsEmpty() else
        if pats.IsEmpty() then false else

        compareArg (Seq.head pats) (Seq.head args) && compareArgsSeq (Seq.tail pats) (Seq.tail args)

    and compareArgs (pats: TreeNodeCollection<_>) (expr: IFSharpExpression) =
        let rec compareArgsRec (expr: IFSharpExpression) i nameUsages =
            let hasMatches = i > 0
            match expr.IgnoreInnerParens() with
            | :? IPrefixAppExpr as app when isNotNull app.ArgumentExpression && i <> pats.Count ->
                let pat = pats.[pats.Count - 1 - i]
                let argExpr = app.ArgumentExpression

                if not (compareArg pat argExpr) then
                    hasMatches, false, app :> IFSharpExpression
                else
                    let funExpr = app.FunctionExpression
                    let usedNames =
                        if isNotNull nameUsages then nameUsages else
                        FSharpNamingService.getUsedNamesUsages funExpr EmptyList.Instance null false

                    if not (patIsUsed usedNames argExpr pat) then
                        compareArgsRec funExpr (i + 1) usedNames
                    else
                        hasMatches, false, app :> _

            | :? IReferenceExpr as r when
                    let name = r.ShortName
                    name = SharedImplUtil.MISSING_DECLARATION_NAME || PrettyNaming.IsOperatorName name ->
                false, false, null

            | x -> hasMatches, i = pats.Count, x

        compareArgsRec expr 0 null

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
            consumer.AddHighlighting(LambdaCanBeReplacedWithInnerExpressionWarning(lambda, replaceCandidate))
        | true, false, replaceCandidate ->
            consumer.AddHighlighting(LambdaCanBeSimplifiedWarning(lambda, replaceCandidate))
        | _ ->

        if pats.Count = 1 then
            match pats.First().IgnoreInnerParens() with
            | :? ITuplePat as pat when pat.PatternsEnumerable.CountIs(2) ->
                let tuplePats = pat.Patterns
                if compareArg (tuplePats.[0]) expr then
                    consumer.AddHighlighting(LambdaCanBeReplacedWithBuiltinFunctionWarning(lambda, "fst"))
                elif compareArg (tuplePats.[1]) expr then
                    consumer.AddHighlighting(LambdaCanBeReplacedWithBuiltinFunctionWarning(lambda, "snd"))
            | _ -> ()

        match compareArg (pats.LastOrDefault()) expr, pats.Count with
        | true, 1 -> consumer.AddHighlighting(LambdaCanBeReplacedWithBuiltinFunctionWarning(lambda, "id"))
        | true, _ -> consumer.AddHighlighting(LambdaBodyCanBeReplacedWithIdWarning(lambda))
        | _ -> ()
