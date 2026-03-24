namespace JetBrains.ReSharper.Plugins.FSharp.Services.Debugger

open FSharp.Compiler.EditorServices
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Debugger
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree

[<Language(typeof<FSharpLanguage>)>]
type FSharpExpressionEvaluationInfoProvider() =
    static let findBetterNode (expr: IFSharpExpression) : IFSharpExpression =
        let prefixAppExpr = PrefixAppExprNavigator.GetByFunctionExpression(expr)
        if isNotNull prefixAppExpr && prefixAppExpr.IsHighPrecedence then
            prefixAppExpr
        else
            expr

    static let rec getTextToEvaluate (expr: IFSharpExpression) =
        match expr with
        | :? IConstExpr as constExpr when not (constExpr :? IUnitExpr) ->
            constExpr.GetText()

        | :? IItemIndexerExpr as indexerExpr ->
            let qualifier = getTextToEvaluate indexerExpr.Qualifier
            if isNull qualifier then null else

            let args = indexerExpr.Args
            let argTexts = args |> Seq.map getTextToEvaluate
            if argTexts |> Seq.exists isNull then null else

            let argsText = String.concat "," argTexts
            qualifier + "[" + argsText + "]"

        | :? IPrefixAppExpr as prefixAppExpr when prefixAppExpr.IsHighPrecedence ->
            let funText = getTextToEvaluate prefixAppExpr.InvokedReferenceExpression
            let argTexts = prefixAppExpr.Arguments |> Seq.map _.As<IFSharpExpression>() |> Seq.map getTextToEvaluate
            let argsText = argTexts |> String.concat ","
            funText + "(" + argsText + ")"

        | :? IReferenceExpr as refExpr ->
            let declaredElement = refExpr.Reference.Resolve().DeclaredElement
            if declaredElement :? ISelfId then "this" else

            let shortName = refExpr.ShortName
            if shortName = SharedImplUtil.MISSING_DECLARATION_NAME then null else

            let qualifier = getTextToEvaluate refExpr.Qualifier
            if isNull qualifier then shortName else qualifier + "." + shortName

        | _ -> null

    static member GetTextToEvaluate(file: IFile, range: DocumentRange) =
        let expr = file.GetNode<IFSharpExpression>(range) |> findBetterNode
        expr, getTextToEvaluate expr

    interface IExpressionEvaluationInfoProvider with
        member x.FindExpression(file, range, _, _, _, _) =
            let expr, textToEvaluate = FSharpExpressionEvaluationInfoProvider.GetTextToEvaluate(file, range)
            if isNotNull textToEvaluate then
                EvaluationExpressionInfo(expr, null, textToEvaluate, expr.GetText()) else

            let tokenOpt =
                let offset = range.StartOffset.Offset
                file.FindTokensAt(TreeTextRange(TreeOffset(offset - 1), TreeOffset(offset + 1)))
                |> Seq.tryFind _.GetTokenType().IsIdentifier

            match tokenOpt with
            | Some(token) ->
                let document = file.GetSourceFile().Document
                let coords = token.GetDocumentStartOffset().ToDocumentCoords()
                let lineText = document.GetLineText(coords.Line)
                match QuickParse.GetCompleteIdentifierIsland false lineText (int coords.Column) with
                | Some(island, _, _) -> EvaluationExpressionInfo(token, null, island, token.GetText()) // todo: compiled names?
                | _ -> null
            | _ -> null
