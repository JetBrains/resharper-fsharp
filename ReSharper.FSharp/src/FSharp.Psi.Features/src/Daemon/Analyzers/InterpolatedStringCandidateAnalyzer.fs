namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.Tree

type StringLiteralSearch(allowSingleQuoteStrings: bool) =
    inherit TreeNodeVisitor<bool, bool>()

    override this.VisitNode (node, found) =
        // todo: there has to be a better way to do this
        node.Children()
        |> Seq.fold
            (fun found child ->
                match child with
                | :? IFSharpTreeNode as treeNode when not found ->
                    treeNode.Accept(this, false)
                | _ -> found)
            found

    override __.VisitLiteralExpr (node, _) =
        let tokenType = getTokenType node.Literal
        if not tokenType.IsStringLiteral then false else

        // Triple quoted strings are never allowed to fill interpolations
        if tokenType = FSharpTokenType.TRIPLE_QUOTED_STRING then true else

        // Triple quoted interpolated strings can contain any other kind of string
        if allowSingleQuoteStrings then false else

        tokenType = FSharpTokenType.STRING ||
        tokenType = FSharpTokenType.VERBATIM_STRING ||
        tokenType = FSharpTokenType.REGULAR_INTERPOLATED_STRING ||
        tokenType = FSharpTokenType.BYTEARRAY

[<ElementProblemAnalyzer([| typeof<IPrefixAppExpr> |],
                         HighlightingTypes = [| typeof<InterpolatedStringCandidateWarning> |])>]
type InterpolatedStringCandidateAnalyzer() =
    inherit ElementProblemAnalyzer<IPrefixAppExpr>()

    override x.Run(prefixAppExpr, data, consumer) =
        if not data.IsFSharp50Supported then () else

        let formatStringExpr = prefixAppExpr.ArgumentExpression.IgnoreInnerParens().As<ILiteralExpr>()
        if isNull formatStringExpr then () else

        let tokenType = getTokenType formatStringExpr.Literal
        if tokenType <> FSharpTokenType.STRING &&
           tokenType <> FSharpTokenType.TRIPLE_QUOTED_STRING &&
           tokenType <> FSharpTokenType.VERBATIM_STRING then ()
        else

        match data.ParseAndCheckResults with
        | None -> ()
        | Some results ->

        // Find all format specifiers in our argument expression
        let matchingFormatSpecsAndArity =
            let document = prefixAppExpr.GetSourceFile().Document
            let argRange = prefixAppExpr.ArgumentExpression.GetHighlightingRange()

            results.CheckResults.GetFormatSpecifierLocationsAndArity()
            |> Seq.map (fun (r, arity) ->
                let textRange = getTextRange document r
                DocumentRange(document, textRange), arity)
            |> Seq.filter (fun (range, _) -> argRange.Contains(&range))
            |> List.ofSeq

        if matchingFormatSpecsAndArity.IsEmpty then () else

        // Interpolated strings only support 1-arity format specifiers
        if matchingFormatSpecsAndArity |> List.exists (fun (_, arity) -> arity <> 1) then () else

        // Find the outermost IPrefixAppExpr and all applied exprs (excluding the format string itself)
        let outerPrefixAppExpr, appliedExprs =
            let rec loop acc (expr: IPrefixAppExpr) =
                match PrefixAppExprNavigator.GetByFunctionExpression (expr.IgnoreParentParens()) with
                | null -> expr.IgnoreParentParens(), acc
                | parent -> loop (parent.ArgumentExpression.IgnoreInnerParens() :: acc) parent
            loop [] prefixAppExpr

        if appliedExprs.Length <> matchingFormatSpecsAndArity.Length then () else

        let allowSingleQuoteStrings = tokenType = FSharpTokenType.TRIPLE_QUOTED_STRING

        // Check that the applied expressions do not contain disallowed string literals
        let visitor = StringLiteralSearch(allowSingleQuoteStrings)
        if appliedExprs |> List.exists (fun expr -> expr.Accept(visitor, false)) then () else

        let formatSpecsAndExprs =
            appliedExprs
            |> Seq.rev
            |> Seq.zip matchingFormatSpecsAndArity
            |> Seq.map (fun ((r, _), expr) -> r, expr)
            |> List.ofSeq

        InterpolatedStringCandidateWarning(formatStringExpr, prefixAppExpr, outerPrefixAppExpr, formatSpecsAndExprs)
        |> consumer.AddHighlighting
