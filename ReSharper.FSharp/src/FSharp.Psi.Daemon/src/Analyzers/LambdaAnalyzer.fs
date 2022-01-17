﻿namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open System
open FSharp.Compiler.Symbols
open FSharp.Compiler.Syntax
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpSymbolUtil
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<ElementProblemAnalyzer(typeof<ILambdaExpr>,
                         HighlightingTypes = [| typeof<LambdaCanBeReplacedWithBuiltinFunctionWarning>
                                                typeof<LambdaCanBeReplacedWithInnerExpressionWarning>
                                                typeof<LambdaCanBeSimplifiedWarning>
                                                typeof<RedundantApplicationWarning> |])>]
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
            not pat.IsStruct = not expr.IsStruct &&
            compareArgsSeq pat.PatternsEnumerable expr.ExpressionsEnumerable

        | :? ILocalReferencePat as pat, (:? IReferenceExpr as expr) ->
            let patReferenceName = pat.ReferenceName
            if patReferenceName.IsQualified || expr.IsQualified then false else

            let patName = patReferenceName.ShortName
            if patName.IsEmpty() || not (Char.IsLower(patName.[0])) then false else

            patName = expr.ShortName

        | :? IUnitPat, :? IUnitExpr -> true
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
                        FSharpNamingService.getUsedNamesUsages [funExpr] EmptyList.Instance null false

                    if not (patIsUsed usedNames argExpr pat) then
                        compareArgsRec funExpr (i + 1) usedNames
                    else
                        hasMatches, false, app :> _

            | :? IReferenceExpr as r when
                    let name = r.ShortName
                    name = SharedImplUtil.MISSING_DECLARATION_NAME || PrettyNaming.IsOperatorDisplayName name ->
                false, false, null

            | x -> hasMatches, i = pats.Count, x

        compareArgsRec expr 0 null

    let isImplicitlyConvertedToDelegate (lambda: ILambdaExpr) =
        let lambda = lambda.IgnoreParentParens()
        let binaryExpr = BinaryAppExprNavigator.GetByRightArgument(lambda)
        let argExpr = if isNull binaryExpr then lambda else binaryExpr :> _
        let appTuple = TupleExprNavigator.GetByExpression(argExpr)
        let app = getArgsOwner argExpr

        let reference = getReference app
        app :? IPrefixAppExpr && isNotNull reference &&

        match reference.GetFcsSymbol() with
        | :? FSharpMemberOrFunctionOrValue as m ->
            m.IsMember &&
            let lambdaPos = if isNotNull appTuple then appTuple.Expressions.IndexOf(argExpr) else 0

            let parameterGroups = m.CurriedParameterGroups
            if parameterGroups.Count = 0 then false else

            let args = parameterGroups.[0]
            if args.Count <= lambdaPos then false else

            let argDecl = args.[lambdaPos]
            let argDeclType = argDecl.Type

            let argIsDelegate =
                argDeclType.HasTypeDefinition && (getAbbreviatedEntity argDeclType.TypeDefinition).IsDelegate

            if argIsDelegate then
                let mApparentEntity = m.ApparentEnclosingEntity
                let mName = m.DisplayName
                let mHasOverload =
                    mApparentEntity.MembersFunctionsAndValues
                    |> Seq.exists (fun x ->
                        not (x.Equals m) &&
                        x.DisplayName = mName &&
                        x.CurriedParameterGroups.[0].Count >= args.Count)
                not mHasOverload
            else false
        | _ -> false

    let isApplicable (expr: IFSharpExpression) (pats: TreeNodeCollection<IFSharpPattern>) =
        match expr with
        | :? IPrefixAppExpr
        | :? IReferenceExpr
        | :? ITupleExpr
        | :? IUnitExpr -> not (pats.Count = 1 && pats.First().IgnoreInnerParens() :? IUnitPat)
        | _ -> false

    override x.Run(lambda, _, consumer) =
        let expr = lambda.Expression.IgnoreInnerParens()
        let pats = lambda.Patterns

        if not (isApplicable expr pats) then () else

        let warning =
            match compareArgs pats expr with
            | true, true, replaceCandidate ->
                LambdaCanBeReplacedWithInnerExpressionWarning(lambda, replaceCandidate) :> IHighlighting
            | true, false, replaceCandidate ->
                LambdaCanBeSimplifiedWarning(lambda, replaceCandidate) :> _
            | _ ->

            if pats.Count = 1 then
                let pat = pats.[0].IgnoreInnerParens()
                if compareArg pat expr then
                    let expr = lambda.IgnoreParentParens()
                    let mutable funExpr = Unchecked.defaultof<_>
                    let mutable arg = Unchecked.defaultof<_>
                    if isFunctionInApp expr &funExpr &arg then
                        RedundantApplicationWarning(funExpr, arg) :> _
                    else
                        LambdaCanBeReplacedWithBuiltinFunctionWarning(lambda, "id") :> _
                else
                    match pat with
                    | :? ITuplePat as pat when not pat.IsStruct && pat.PatternsEnumerable.CountIs(2) ->
                        let tuplePats = pat.Patterns
                        if compareArg tuplePats.[0] expr then
                            LambdaCanBeReplacedWithBuiltinFunctionWarning(lambda, "fst") :> _
                        elif compareArg tuplePats.[1] expr then
                            LambdaCanBeReplacedWithBuiltinFunctionWarning(lambda, "snd") :> _
                        else null
                    | _ -> null

            else null

        if isNotNull warning && not (isImplicitlyConvertedToDelegate lambda) then
            consumer.AddHighlighting(warning)
