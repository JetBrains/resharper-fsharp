namespace JetBrains.ReSharper.Plugins.FSharp.Services.Debugger

open FSharp.Compiler.EditorServices
open JetBrains.ReSharper.Feature.Services.Debugger
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree

[<Language(typeof<FSharpLanguage>)>]
type FSharpExpressionEvaluationInfoProvider() =
    static let rec getTextToEvaluate (expr: IFSharpExpression) =
        match expr with
        | :? IReferenceExpr as refExpr ->
            let declaredElement = refExpr.Reference.Resolve().DeclaredElement
            if declaredElement :? ISelfId then "this" else

            let shortName = refExpr.ShortName
            if shortName = SharedImplUtil.MISSING_DECLARATION_NAME then null else

            let qualifier = getTextToEvaluate refExpr.Qualifier
            if isNull qualifier then shortName else qualifier + "." + shortName

        | :? IConstExpr as constExpr when not (constExpr :? IUnitExpr) ->
            constExpr.GetText()

        | :? IItemIndexerExpr as indexerExpr ->
            let qualifier = getTextToEvaluate indexerExpr.Qualifier
            if isNull qualifier then null else

            let getArgText (arg: IIndexerArg) =
                let argExpr = arg.As<IIndexerArgExpr>()
                if isNull argExpr then null else getTextToEvaluate argExpr.Expression

            let args = indexerExpr.Args
            let argsText = args |> Seq.map getArgText
            if argsText |> Seq.exists isNull then null else

            let argsText = String.concat "," argsText
            qualifier + "[" + argsText + "]"            

        | _ -> null

    static member GetTextToEvaluate(expr: IFSharpExpression) =
        getTextToEvaluate expr

    interface IExpressionEvaluationInfoProvider with
        member x.FindExpression(file, range, _, _) =
            let expr = file.GetNode<IFSharpExpression>(range)
            let exprName = getTextToEvaluate expr
            if isNotNull exprName then
                EvaluationExpressionInfo(expr, null, exprName, expr.GetText())
            else
                let offset = range.StartOffset.Offset
                let tokenOpt =
                    file.FindTokensAt(TreeTextRange(TreeOffset(offset - 1), TreeOffset(offset + 1)))
                    |> Seq.tryFind (fun t -> t.GetTokenType().IsIdentifier)
                match tokenOpt with
                | Some(token) ->
                    let document = file.GetSourceFile().Document
                    let coords = document.GetCoordsByOffset(token.GetTreeStartOffset().Offset)
                    let lineText = document.GetLineText(coords.Line)
                    match QuickParse.GetCompleteIdentifierIsland false lineText (int coords.Column) with
                    | Some(island, _, _) -> EvaluationExpressionInfo(token, null, island, token.GetText()) // todo: compiled names?
                    | _ -> null
                | _ -> null
