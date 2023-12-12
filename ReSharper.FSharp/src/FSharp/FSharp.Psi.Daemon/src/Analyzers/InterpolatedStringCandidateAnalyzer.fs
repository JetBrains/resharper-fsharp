namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open FSharp.Compiler.Symbols
open FSharp.Compiler.Text
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

module InterpolatedStringCandidateAnalyzer =
    let formatSpecifiersKey = Key<(range * int)[]>("FormatSpecifiersKey")

[<ElementProblemAnalyzer([| typeof<ILiteralExpr> |],
                         HighlightingTypes = [| typeof<InterpolatedStringCandidateWarning> |])>]
type InterpolatedStringCandidateAnalyzer() =
    inherit ElementProblemAnalyzer<ILiteralExpr>()

    let getFormatSpecifierLocationsAndArity (checkResults: FSharpParseAndCheckResults) (data: ElementProblemAnalyzerData) =
        data.GetOrCreateDataUnderLock(InterpolatedStringCandidateAnalyzer.formatSpecifiersKey, fun _ ->
            checkResults.CheckResults.GetFormatSpecifierLocationsAndArity()
        )

    let isDisallowedStringLiteral (literalExpr: ILiteralExpr) =
        let tokenType = getTokenType literalExpr.Literal
        tokenType == FSharpTokenType.STRING ||
        tokenType == FSharpTokenType.BYTEARRAY ||
        tokenType == FSharpTokenType.VERBATIM_STRING ||
        tokenType == FSharpTokenType.TRIPLE_QUOTED_STRING ||
        tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING

    override x.Run(literalExpr, data, consumer) =
        if not data.IsFSharp50Supported || (data.FSharpCoreVersion.Major < 5 && not Shell.Instance.IsTestShell) then () else

        let prefixAppExpr = PrefixAppExprNavigator.GetByArgumentExpression(literalExpr.IgnoreParentParens())
        if isNull prefixAppExpr then () else

        let tokenType = getTokenType literalExpr.Literal
        if tokenType != FSharpTokenType.STRING &&
           tokenType != FSharpTokenType.TRIPLE_QUOTED_STRING &&
           tokenType != FSharpTokenType.VERBATIM_STRING then () else

        match data.ParseAndCheckResults with
        | None -> ()
        | Some checkResults ->

        // Find all format specifiers in our argument expression
        let matchingFormatSpecsAndArity =
            let document = prefixAppExpr.GetSourceFile().Document
            let argRange = prefixAppExpr.ArgumentExpression.GetHighlightingRange()

            getFormatSpecifierLocationsAndArity checkResults data
            |> Seq.map (fun (r, arity) ->
                let textRange = getTextRange document r
                DocumentRange(document, textRange), arity)
            |> Seq.filter (fun (range, _) -> argRange.Contains(range))
            |> Seq.distinct
            |> List.ofSeq

        if matchingFormatSpecsAndArity.IsEmpty then () else

        // Interpolated strings only support 1-arity format specifiers
        if matchingFormatSpecsAndArity |> List.exists (fun (_, arity) -> arity <> 1) then () else

        let funReference = prefixAppExpr.InvokedFunctionReference
        if isNull funReference then () else

        let mfv = funReference.GetFcsSymbol().As<FSharpMemberOrFunctionOrValue>()
        if isNull mfv then () else

        let parameterGroups = mfv.CurriedParameterGroups
        if prefixAppExpr.AppliedExpressions.Count <> parameterGroups.Count then () else

        // Find the outermost IPrefixAppExpr and all applied exprs (excluding the format string itself)
        let outerPrefixAppExpr, appliedExprs =
            let getArgExpr (expr: IFSharpExpression) =
                match expr.IgnoreInnerParens() with
                | :? ITupleExpr as tupleExpr when
                        tupleExpr != expr && not tupleExpr.IsStruct ->
                    tupleExpr.Parent :?> IFSharpExpression
                | expr -> expr

            let rec loop acc (expr: IPrefixAppExpr) =
                match PrefixAppExprNavigator.GetByFunctionExpression (expr.IgnoreParentParens()) with
                | null -> expr.IgnoreParentParens(), acc
                | parent -> loop (getArgExpr parent.ArgumentExpression :: acc) parent

            loop [] prefixAppExpr

        if appliedExprs.Length <> matchingFormatSpecsAndArity.Length then () else

        // Check that the applied expressions do not contain disallowed string literals
        let anyDisallowedExprs =
            let isDisallowed =
                if tokenType == FSharpTokenType.TRIPLE_QUOTED_STRING then
                    fun (literalExpr: ILiteralExpr) ->
                        getTokenType literalExpr.Literal = FSharpTokenType.TRIPLE_QUOTED_STRING
                else
                    isDisallowedStringLiteral

            appliedExprs
            |> List.exists (fun expr ->
                expr.ThisAndDescendants<ILiteralExpr>().ToEnumerable() |> Seq.exists isDisallowed)

        if anyDisallowedExprs then () else

        let formatSpecsAndExprs =
            appliedExprs
            |> Seq.rev
            |> Seq.zip matchingFormatSpecsAndArity
            |> Seq.map (fun ((r, _), expr) -> r, expr)
            |> List.ofSeq

        InterpolatedStringCandidateWarning(literalExpr, prefixAppExpr, outerPrefixAppExpr, formatSpecsAndExprs)
        |> consumer.AddHighlighting
