namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.StringLiteralsUtil
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Text

type ReplaceWithTripleQuotedInterpolatedStringFix(error: SingleQuoteInSingleQuoteError) =
    inherit FSharpQuickFixBase()

    let createStart content =
        FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_START.Create($"$\"\"\"{content}{{")

    let createMiddle content =
        FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE.Create($"}}{content}{{")

    let createEnd content =
        FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END.Create($"}}{content}\"\"\"")

    let checkRegularStringLiteral (literal: ITokenNode) =
        let tokenContent = getStringContent (literal.GetTokenType()) (literal.GetText())
        let lexer = RegularInterpolatedStringLexer(StringBuffer(tokenContent))

        let mutable found = false
        while not found && lexer.CanAdvance do
            lexer.Advance()
            found <- lexer.TokenType == StringTokenTypes.ESCAPE_CHARACTER
        found

    let processStringLiteral (textContentFactory: TokenNodeType -> string -> string) (literal: ITokenNode) =
        let text = literal.GetText()
        let tokenType = literal.GetTokenType()
        let content = textContentFactory tokenType text

        let resultingNode: ITreeNode =
            match tokenType with
            | tokenType when FSharpTokenType.InterpolatedStringsStart[tokenType] -> createStart content
            | tokenType when FSharpTokenType.InterpolatedStringsMiddle[tokenType] -> createMiddle content
            | tokenType when FSharpTokenType.InterpolatedStringsEnd[tokenType] -> createEnd content
            | _ -> literal

        if literal != resultingNode then
            ModificationUtil.ReplaceChild(literal, resultingNode) |> ignore

    let regularStringContentFactory (tokenType: TokenNodeType) (text: string)  =
        getStringContent tokenType text

    let verbatimStringContentFactory (tokenType: TokenNodeType) (text: string) =
        (getStringContent tokenType text).Replace("\"\"", "\"")

    override this.IsAvailable _ =
        if not <| isValid error.Expr then false else

        let parentExpr = error.Expr.GetContainingNode<IInterpolatedStringExpr>()
        if isNull parentExpr then false else

        // Nested triple quoted interpolated strings represent not valid F# code, so ignore such possible case
        let grandparentExpr = parentExpr.GetContainingNode<IInterpolatedStringExpr>()
        if isNotNull grandparentExpr then false else

        // Only regular interpolated strings without escape characters are supported
        if parentExpr.FirstChild.GetTokenType() != FSharpTokenType.REGULAR_INTERPOLATED_STRING_START then true else
        let containsEscapeCharacter = parentExpr.LiteralsEnumerable |> Seq.exists checkRegularStringLiteral
        not containsEscapeCharacter

    override this.Text = "Replace with triple-quoted interpolated string"

    override this.ExecutePsiTransaction (_: ISolution) =
        let interpolatedExpr = error.Expr.GetContainingNode<IInterpolatedStringExpr>()

        use _ = WriteLockCookie.Create()

        let firstChildType = interpolatedExpr.FirstChild.GetTokenType()
        if firstChildType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_START then
            interpolatedExpr.Literals
            |> Seq.iter (processStringLiteral regularStringContentFactory)
        else if firstChildType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING_START then
            interpolatedExpr.Literals
            |> Seq.iter (processStringLiteral verbatimStringContentFactory)
