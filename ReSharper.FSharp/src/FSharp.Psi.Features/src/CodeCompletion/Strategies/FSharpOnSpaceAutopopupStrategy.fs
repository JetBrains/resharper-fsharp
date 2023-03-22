namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Strategies

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

// todo: add F# specific settings

[<SolutionComponent>]
type FSharpOnSpaceAutopopupStrategy() =
    interface IAutomaticCodeCompletionStrategy with
        member this.Language = FSharpLanguage.Instance
        member this.ForceHideCompletion = true
        member this.ProcessSubsequentTyping(_, _) = false
        member this.IsEnabledInSettings(_, _) = AutopopupType.SoftAutopopup

        member this.AcceptTyping(c, _, _) = c = ' '

        member this.AcceptsFile(file, textControl) =
            AutomaticCodeCompletionStrategyEx.MatchToken(this, file, textControl, fun (token: ITokenNode) ->
                let prevToken = token.GetPreviousToken()
                match prevToken with
                | TokenType FSharpTokenType.BAR bar ->
                    match bar.Parent with
                    | :? IMatchClause as matchClause -> matchClause.Bar == bar
                    | :? IOrPat as orPat -> orPat.Bar == bar

                    | :? IMatchLikeExpr
                    | :? IParenPat -> true

                    | _ -> false

                | TokenType FSharpTokenType.SEMICOLON semi ->
                    match semi.Parent with
                    | :? IRecordFieldBinding as fieldBinding ->
                        fieldBinding.Semicolon == semi

                    | _ -> false

                | TokenType FSharpTokenType.LBRACE lbrace ->
                    match lbrace.Parent with
                    | :? IRecordExpr as recordExpr ->
                        recordExpr.LeftBrace == lbrace

                    | _ -> false

                | _ -> false
            )
