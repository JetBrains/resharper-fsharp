namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Text
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type private StringManipulation =
    | InsertInterpolation of formatSpecifier: string * exprText: string
    | EscapeBrace of braceChar: char

type ReplaceWithInterpolatedStringFix(warning: InterpolatedStringCandidateWarning) =
    inherit FSharpScopedQuickFixBase()

    let outerPrefixAppExpr = warning.OuterPrefixAppExpr
    let prefixAppExpr = warning.PrefixAppExpr
    let formatStringExpr = warning.FormatStringExpr

    override this.Text = "To interpolated string"

    override this.IsAvailable _ =
        isValid formatStringExpr

    override this.TryGetContextTreeNode() = formatStringExpr :> _

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(formatStringExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let appliedExprFormatSpecs =
            let startOffset = formatStringExpr.GetNavigationRange().StartOffset.Offset

            warning.FormatSpecsAndExprs
            |> Seq.map (fun (specifierRange, expr) ->
                let index = specifierRange.EndOffset.Offset - startOffset
                index, InsertInterpolation (specifierRange.GetText(), expr.GetText()))

        let formatString = formatStringExpr.GetText()

        let bracesToEscape =
            formatString
            |> Seq.indexed
            |> Seq.filter (fun (_, c) -> c = '{' || c = '}')
            |> Seq.map (fun (i, c) -> i, EscapeBrace(c))

        // Order text manipulation operations in reverse (end of string -> start)
        // This ensures manipulations don't affect the index of subsequent manipulations
        let manipulations =
            appliedExprFormatSpecs
            |> Seq.append bracesToEscape
            |> Seq.sortByDescending fst
            |> List.ofSeq

        let interpolatedSb =
            let extraCapacity =
                manipulations
                |> Seq.sumBy (function
                    | _, EscapeBrace _ -> 1
                    | _, InsertInterpolation(_, exprText) -> 2 + exprText.Length)

            StringBuilder(formatString, formatString.Length + 1 + extraCapacity)

        for index, manipulation in manipulations do
            match manipulation with
            | EscapeBrace(braceChar) ->
                interpolatedSb.Insert(index, braceChar) |> ignore
            | InsertInterpolation(formatSpecifier, exprText) ->
                let index =
                    // %O is the implied default in interpolated strings
                    if formatSpecifier = "%O" then
                        interpolatedSb.Remove(index - 2, 2) |> ignore
                        index - 2
                    else
                        index

                interpolatedSb
                    .Insert(index, '{')
                    .Insert(index + 1, exprText)
                    .Insert(index + 1 + exprText.Length, '}')
                |> ignore

        let factory = formatStringExpr.CreateElementFactory()

        interpolatedSb.Insert(0, '$') |> ignore
        let interpolatedStringExpr = factory.CreateExpr(interpolatedSb.ToString())

        if isPredefinedFunctionRef "sprintf" prefixAppExpr.FunctionExpression then
            ModificationUtil.ReplaceChild(outerPrefixAppExpr, interpolatedStringExpr) |> ignore
        else
            ModificationUtil.ReplaceChild(formatStringExpr, interpolatedStringExpr) |> ignore
            ModificationUtil.ReplaceChild(outerPrefixAppExpr, prefixAppExpr) |> ignore
