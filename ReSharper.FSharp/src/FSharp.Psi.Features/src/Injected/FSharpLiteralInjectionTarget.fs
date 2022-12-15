namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected

open System.Text.RegularExpressions
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.CSharp.Util.Literals
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.StringLiteralsUtil
open JetBrains.ReSharper.Psi.RegExp.ClrRegex
open JetBrains.ReSharper.Psi.RegExp.ClrRegex.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi
open JetBrains.Text

[<SolutionComponent>]
type FSharpLiteralInjectionTarget() =

    let tryOpenChameleon (node: ITreeNode) =
        match node with
        | :? IChameleonExpression as expr when expr.IsLiteralExpression() ->
            expr.FirstChild |> ignore
        | _ -> ()

    interface IInjectionTargetLanguage with
        override _.ShouldInjectByAnnotation(_, prefix, postfix) =
            prefix <- null
            postfix <- null
            false

        override _.GetStartOffsetForString(originalNode) =
            originalNode.GetTokenType() |> getStringStartingQuotesLength

        override _.GetEndOffsetForString(originalNode) =
            originalNode.GetTokenType() |> getStringEndingQuotesLength

        override _.UpdateNode(_, _, _, length, _, _, _, _) =
            length <- -1
            null

        override _.SupportsRegeneration = false

        override _.IsInjectionAllowed(node) =
            tryOpenChameleon node

            let tokenType = node.GetTokenType()

            FSharpTokenType.Strings[tokenType] &&
            tokenType <> FSharpTokenType.CHARACTER_LITERAL &&
            tokenType <> FSharpTokenType.VERBATIM_BYTEARRAY &&
            tokenType <> FSharpTokenType.BYTEARRAY

        override _.GetCorrespondingCommentTextForLiteral _ = null

        override _.CreateBuffer(literalNode, text, options) =
            let literalType =
                match literalNode.GetTokenType().GetLiteralType() with
                | RegularString ->
                    CSharpLiteralType.RegularString :> CSharpLiteralType
                | InterpolatedString ->
                    CSharpLiteralType.RegularInterpolatedString
                | InterpolatedStringStart ->
                    CSharpLiteralType.RegularInterpolatedStringStart
                | InterpolatedStringMiddle ->
                    CSharpLiteralType.RegularInterpolatedStringMiddle
                | InterpolatedStringEnd ->
                    CSharpLiteralType.RegularInterpolatedStringEnd
                | _ ->
                    CSharpLiteralType.VerbatimString

            let lexerOptions =
                match options with
                | :? RegexOptions as regexOptions when regexOptions.HasFlag(RegexOptions.IgnorePatternWhitespace) ->
                    ClrRegexLexerOptions(true)
                | _ ->
                    ClrRegexLexerOptions.None

            CSharpRegExpBuffer(StringBuffer(text), literalType, lexerOptions) :> _

        override _.DoNotProcessNodeInterior _ = false

        override _.IsPrimaryLanguageApplicable(sourceFile) =
            sourceFile.LanguageType.Is<FSharpProjectFileType>()

        override _.CreateLexerFactory(languageService) =
            languageService.GetPrimaryLexerFactory()

        override _.AllowsLineBreaks _ = true

        override _.IsWhitespaceToken(token) =
            token.GetTokenType().IsWhitespace

        override x.FixValueRangeForLiteral(element) =
            element.GetTreeTextRange()

        override _.Language = FSharpLanguage.Instance
