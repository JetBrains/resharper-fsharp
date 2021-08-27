namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Injected

open System
open System.Text.RegularExpressions
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi.CSharp.Util.Literals
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.StringLiteralsUtil
open JetBrains.ReSharper.Psi.RegExp.ClrRegex
open JetBrains.ReSharper.Psi.RegExp.ClrRegex.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi
open JetBrains.Text

[<SolutionComponent>]
type FSharpLiteralInjectionTarget() =
    interface IInjectionTargetLanguage with
        override _.ShouldInjectByAnnotation(_, prefix, postfix) =
            prefix <- null
            postfix <- null
            false

        override _.GetStartOffsetForString(originalNode) =
            match originalNode.GetTokenType().GetLiteralType() with
            | FSharpLiteralType.VerbatimString -> 2
            | _ -> 1

        override _.GetEndOffsetForString _ = 1

        override _.UpdateNode(_, _, _, length, _, _, _, _) =
            length <- -1
            null

        override _.SupportsRegeneration = false

        override _.IsInjectionAllowed(literalNode) =
            let tokenType = literalNode.GetTokenType()
            if isNull tokenType || not tokenType.IsStringLiteral then false else

            match tokenType.GetLiteralType() with
            | FSharpLiteralType.VerbatimString
            | FSharpLiteralType.RegularString -> true
            | _ -> false

        override _.GetCorrespondingCommentTextForLiteral _ = null

        override _.CreateBuffer(_, text, options) =
            let literalType =
                if text.StartsWith("@", StringComparison.Ordinal) then CSharpLiteralType.VerbatimString
                else CSharpLiteralType.RegularString

            let lexerOptions =
                match options with
                | :? RegexOptions as regexOptions when regexOptions.HasFlag(RegexOptions.IgnorePatternWhitespace) ->
                    ClrRegexLexerOptions(true)
                | _ ->
                    ClrRegexLexerOptions.None

            CSharpRegExpBuffer(StringBuffer(text), literalType, lexerOptions) :> _

        override _.DoNotProcessNodeInterior _ = false

        override _.IsPrimaryLanguageApplicable(sourceFile) =
            match sourceFile.LanguageType with
            | :? FSharpProjectFileType -> true
            | _ -> false

        override _.CreateLexerFactory(languageService) =
            languageService.GetPrimaryLexerFactory()

        override _.AllowsLineBreaks(literalNode) =
            match literalNode.GetTokenType().GetLiteralType() with
            | FSharpLiteralType.VerbatimString -> true
            | _ -> false

        override _.IsWhitespaceToken(token) =
            token.GetTokenType().IsWhitespace

        override x.FixValueRangeForLiteral(element) =
            let startOffset = (x :> IInjectionTargetLanguage).GetStartOffsetForString(element)
            element.GetTreeTextRange().TrimLeft(startOffset).TrimRight(1)

        override _.Language = FSharpLanguage.Instance :> _
