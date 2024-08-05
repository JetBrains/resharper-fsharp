namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected

open System.Text.RegularExpressions
open JetBrains.Application.Parts
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Psi.CSharp.Util.Literals
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.StringLiteralsUtil
open JetBrains.ReSharper.Psi.RegExp.ClrRegex
open JetBrains.ReSharper.Psi.RegExp.ClrRegex.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi
open JetBrains.Text

[<SolutionComponent(InstantiationEx.LegacyDefault)>]
type FSharpLiteralInjectionTarget() =
    let isInjectionAllowed tokenType =
        FSharpTokenType.Strings[tokenType] &&
        tokenType <> FSharpTokenType.CHARACTER_LITERAL &&
        tokenType <> FSharpTokenType.VERBATIM_BYTEARRAY &&
        tokenType <> FSharpTokenType.BYTEARRAY

    interface IInjectionTargetLanguage with
        override _.ShouldInjectByAnnotation(_, prefix, postfix) =
            prefix <- null
            postfix <- null
            false

        override _.GetStartOffsetForString(originalNode) =
            let token = originalNode :?> ITokenNode
            getStringStartingQuotesLength token

        override _.GetEndOffsetForString(originalNode) =
            let token = originalNode :?> ITokenNode
            getStringEndingQuotesLength token

        override _.UpdateNode(_, _, _, length, _, _, _, _) =
            length <- -1
            null

        override _.SupportsRegeneration = false
        override _.LiteralBorderCharacters = [|'"'|]

        override x.IsInjectionAllowed(tokenType) =
            isInjectionAllowed tokenType

        override x.IsInjectionAllowed(node: ITreeNode) =
            let tokenType = node.GetTokenType()
            isInjectionAllowed tokenType

        override _.GetCorrespondingCommentTextForLiteral _ = null

        // TODO: Implement FSharpRegExpBuffer & add needed abstractions to ReSharper
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

        override _.ContainsInjectableLiterals(node) =
            match node with
            | :? IChameleonExpression as e when e.IsLiteralExpression() -> InjectableLiteralsPresence.Yes
            | _ -> InjectableLiteralsPresence.Maybe

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
