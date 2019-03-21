namespace JetBrains.ReSharper.Plugins.FSharp.Services.Debugger

open FSharp.Compiler
open JetBrains.ReSharper.Feature.Services.Debugger
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

[<Language(typeof<FSharpLanguage>)>]
type FSharpExpressionEvaluationInfoProvider() =
    interface IExpressionEvaluationInfoProvider with
        member x.FindExpression(file, range, _) =
            let offset = range.StartOffset.Offset
            let tokenOpt =
                file.FindTokensAt(TreeTextRange(TreeOffset(offset - 1), TreeOffset(offset + 1)))
                |> Seq.tryFind (fun t -> t.GetTokenType().IsIdentifier)
            match tokenOpt with
            | Some token ->
                let document = file.GetSourceFile().Document
                let coords = document.GetCoordsByOffset(token.GetTreeStartOffset().Offset)
                let lineText = document.GetLineText(coords.Line)
                match QuickParse.GetCompleteIdentifierIsland false lineText (int (coords.Column)) with
                | Some (island, _, _) -> EvaluationExpressionInfo(token, island, token.GetText()) // todo: compiled names?
                | _ -> null
            | _ -> null
