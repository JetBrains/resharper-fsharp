namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Strategies

open JetBrains.Application.Settings
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.TextControl
open JetBrains.Util

// todo: add F# specific settings

[<AbstractClass>]
type FSharpAutopopupStrategyBase() =
    abstract AcceptTyping: c: char * textControl: ITextControl * settingsStore: IContextBoundSettingsStore -> bool

    abstract AcceptsFile: file: IFile * textControl: ITextControl -> bool
    default this.AcceptsFile(file, _) = file :? IFSharpFile

    abstract ProcessSubsequentTyping: c: char * textControl: ITextControl -> bool
    default this.ProcessSubsequentTyping(_, _) = false

    interface IAutomaticCodeCompletionStrategy with
        member this.Language = FSharpLanguage.Instance
        member this.ForceHideCompletion = false
        member this.IsEnabledInSettings(_, _) = AutopopupType.SoftAutopopup
        member this.AcceptTyping(c, textControl, settingsStore) = this.AcceptTyping(c, textControl, settingsStore)
        member this.AcceptsFile(file, textControl) = this.AcceptsFile(file, textControl)
        member this.ProcessSubsequentTyping(c, textControl) = this.ProcessSubsequentTyping(c, textControl)


[<SolutionComponent>]
type FSharpAutocompletionStrategy() =
    inherit FSharpAutopopupStrategyBase()

    override x.AcceptTyping(char, _, _) = char.IsLetterFast() || char = '.'
    override x.ProcessSubsequentTyping(char, _) = char.IsIdentifierPart()


[<SolutionComponent>]
type FSharpOnSpaceAutopopupStrategy() =
    inherit FSharpAutopopupStrategyBase()

    override this.AcceptTyping(c, _, _) = c = ' '

    override this.AcceptsFile(file, textControl) =
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

            | TokenType FSharpTokenType.IDENTIFIER identifier ->
                match identifier.Parent with
                | :? IExpressionReferenceName as referenceName ->
                    referenceName.Identifier == identifier &&

                    let refPat = LocalReferencePatNavigator.GetByReferenceName(referenceName)
                    isNotNull refPat &&

                    isNull refPat.Binding &&
                    isNull (BindingNavigator.GetByParameterPattern(refPat))

                | _ -> false

            | _ -> false
        )


[<SolutionComponent>]
type FSharpOnParenAutopopupStrategy() =
    inherit FSharpAutopopupStrategyBase()

    override this.AcceptTyping(c, _, _) = c = '('

    override this.AcceptsFile(file, textControl) =
        AutomaticCodeCompletionStrategyEx.MatchToken(this, file, textControl, fun (token: ITokenNode) ->
            let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(token.Parent.As<IUnitPat>())
            isNotNull parametersOwnerPat
        )
