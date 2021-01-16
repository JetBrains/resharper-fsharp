namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open System.Text
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Feature.Services.Daemon

type private StringManipulation =
    | InsertInterpolation of specifier: string * expr: IFSharpExpression
    | EscapeBrace of braceChar: char

[<ContextAction(Name = "ToInterpolatedString", Group = "F#",
                Description = "Convert an printf-style format string with an interpolated string")>]
type ToInterpolatedStringAction(dataProvider: FSharpContextActionDataProvider) =
    inherit FSharpContextActionBase(dataProvider)

    let [<Literal>] opName = "ToInterpolatedStringAction"

    override x.Text = "To interpolated string"

    override x.IsAvailable _ =
        let prefixAppExpr = dataProvider.GetSelectedElement<IPrefixAppExpr>()
        if isNull prefixAppExpr then false else

        // todo: fix NRE in tests
        //if not (FSharpLanguageLevel.isFSharp50Supported prefixAppExpr) then false else

        let literalExpr = prefixAppExpr.ArgumentExpression.IgnoreInnerParens().As<ILiteralExpr>()
        if isNull literalExpr then false else

        // todo: support FSharpTokenType.TRIPLE_QUOTED_STRING and FSharpTokenType.VERBATIM_STRING
        let tokenType = literalExpr.Literal.GetTokenType()
        if tokenType <> FSharpTokenType.STRING then false else

        let sourceFile = prefixAppExpr.GetSourceFile()
        match prefixAppExpr.CheckerService.ParseAndCheckFile(sourceFile, opName, true) with
        | None -> false
        | Some results ->

        // Find all format specifiers in our argument expression
        let argRange = prefixAppExpr.ArgumentExpression.GetHighlightingRange()
        let matchingFormatSpecifiers =
            results.CheckResults.GetFormatSpecifierLocationsAndArity()
            |> Seq.filter (fun (r, _) ->
                let range = getDocumentRange sourceFile.Document r
                argRange.Contains(&range)
            )
            |> List.ofSeq

        match matchingFormatSpecifiers with
        | [] -> false
        | specifiers ->
            // Interpolated strings only support 1-arity format specifiers
            specifiers |> List.forall (fun (r, n) -> n = 1)

    override x.ExecutePsiTransaction _ =
        let prefixAppExpr = dataProvider.GetSelectedElement<IPrefixAppExpr>()
        let literalExpr = prefixAppExpr.ArgumentExpression.IgnoreInnerParens().As<ILiteralExpr>()

        let sourceFile = prefixAppExpr.GetSourceFile()
        let document = sourceFile.Document
        match prefixAppExpr.CheckerService.ParseAndCheckFile(sourceFile, opName, true) with
        | None -> ()
        | Some results ->

        let matchingFormatSpecs =
            let argRange = prefixAppExpr.ArgumentExpression.GetHighlightingRange()

            results.CheckResults.GetFormatSpecifierLocationsAndArity()
            |> Seq.map (fun (r, _) ->
                let textRange = getTextRange document r
                DocumentRange(document, textRange), document.GetText(textRange)
            )
            |> Seq.filter (fun (range, _) -> argRange.Contains(&range))
            |> Array.ofSeq

        // Find the outermost IPrefixAppExpr and all applied exprs (excluding the format string itself)
        let outerPrefixAppExpr, appliedExprs =
            let rec loop acc (expr: IPrefixAppExpr) =
                match PrefixAppExprNavigator.GetByFunctionExpression (expr.IgnoreParentParens()) with
                | null -> expr.IgnoreParentParens(), acc
                | parent -> loop (parent.ArgumentExpression::acc) parent
            loop [] prefixAppExpr

        if appliedExprs.Length <> matchingFormatSpecs.Length then () else

        let appliedExprFormatSpecs =
            let startOffset = literalExpr.GetNavigationRange().StartOffset.Offset + 1

            matchingFormatSpecs
            |> Seq.map (fun (specifierRange, text) -> specifierRange.EndOffset.Offset - startOffset, text)
            |> Seq.rev
            |> Seq.zip appliedExprs
            |> Seq.map (fun (expr, (i, text)) -> i, InsertInterpolation (text, expr))

        // Get the contents of the format string, excluding quotes
        let formatString =
            let t = literalExpr.GetText()
            t.Substring(1, t.Length - 2)

        let bracesToEscape =
            formatString
            |> Seq.indexed
            |> Seq.filter (fun (i, c) -> c = '{' || c = '}')
            |> Seq.map (fun (i, c) -> i, EscapeBrace c)

        // Order text manipulation operations in reverse (end of string -> start)
        // This ensures manipulations don't affect the index of subsequent manipulations
        let manipulations =
            appliedExprFormatSpecs
            |> Seq.append bracesToEscape
            |> Seq.sortByDescending fst
            |> List.ofSeq

        let interpolatedString = StringBuilder(formatString)
        for index, manipulation in manipulations do
            match manipulation with
            | EscapeBrace braceChar ->
                interpolatedString.Insert(index, braceChar) |> ignore
            | InsertInterpolation (text, expr) ->
                let index =
                    // %O is the implied default in interpolated strings
                    if text = "%O" then
                        interpolatedString.Remove(index - 2, 2) |> ignore
                        index - 2
                    else
                        index

                let exprText = expr.GetText()
                interpolatedString
                    .Insert(index, '{')
                    .Insert(index + 1, exprText)
                    .Insert(index + 1 + exprText.Length, '}')
                |> ignore

        use writeCookie = WriteLockCookie.Create(prefixAppExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let factory = literalExpr.CreateElementFactory()
        let interpolatedStringExpr = factory.CreateInterpolatedString(interpolatedString.ToString())

        // todo: use isPredefinedFunctionRef instead
        if prefixAppExpr.Reference.GetName() = "sprintf" then
            ModificationUtil.ReplaceChild(outerPrefixAppExpr, interpolatedStringExpr) |> ignore
        else
            ModificationUtil.ReplaceChild(literalExpr, interpolatedStringExpr) |> ignore
            ModificationUtil.ReplaceChild(outerPrefixAppExpr, prefixAppExpr) |> ignore
