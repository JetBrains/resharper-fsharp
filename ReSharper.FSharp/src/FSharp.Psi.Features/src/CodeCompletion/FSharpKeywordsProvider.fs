namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open FSharp.Compiler.SourceCodeServices
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Host.Features.Completion
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Psi.Tree
open JetBrains.TextControl
open JetBrains.UI.RichText
open JetBrains.Util

[<RequireQualifiedAccess>]
type KeywordSuffix =
    | Quotes
    | Space
    | None

[<Language(typeof<FSharpLanguage>)>]
type FSharpKeywordsProvider() =
    inherit FSharpItemsProviderBase()

    let lookupItems =
        lazy
            let hashDirectives =
                [| KeywordSuffix.Quotes, [| "#load"; "#r"; "#I"; "#nowarn"; "#time" |]
                   KeywordSuffix.None, [| "#if"; "#else"; "#endif" |] |]
                |> Array.map (fun (suffix, directives) ->
                    directives |> Array.map (fun d -> FSharpHashDirectiveLookupItem(d, suffix) :> TextLookupItemBase))
                |> Array.concat

            let keywords =
                Keywords.KeywordsWithDescription
                // todo: implement auto-popup completion strategy that will cover operators
                |> List.filter (fun (keyword, _) -> not (PrettyNaming.IsOperatorName keyword))
                |> List.map (fun (keyword, description) ->
                    FSharpKeywordLookupItem(keyword, description, KeywordSuffix.None) :> TextLookupItemBase)
                |> Array.ofList

            Array.append keywords hashDirectives

    member x.LookupItems = lookupItems.Value

    override x.IsAvailable _ = true

    override x.AddLookupItems(context, collector) =
        match context.TokenBeforeCaret with
        | null -> false
        | tokenBefore ->

        match context.FsCompletionContext, tokenBefore.GetTokenType() with
        | Some (CompletionContext.Invalid), tokenBeforeType when tokenBeforeType != FSharpTokenType.HASH -> false
        | _, tokenBeforeType ->

        if tokenBeforeType == FSharpTokenType.LINE_COMMENT ||
           tokenBeforeType == FSharpTokenType.DEAD_CODE ||
           tokenBeforeType == FSharpTokenType.DOT ||
           tokenBeforeType == FSharpTokenType.RESERVED_LITERAL_FORMATS ||
           isNotNull tokenBeforeType && tokenBeforeType.IsConstantLiteral ||
           tokenBefore == context.TokenAtCaret && isNotNull tokenBeforeType &&
               (tokenBeforeType.IsComment || tokenBeforeType.IsStringLiteral || tokenBeforeType.IsConstantLiteral)
        then false else

        if not context.PartialLongName.QualifyingIdents.IsEmpty then false else

        for item in x.LookupItems do
            item.InitializeRanges(context.Ranges, context.BasicContext)
            collector.Add(item)

        true


type FSharpKeywordLookupItemBase(keyword, keywordSuffix) =
    inherit TextLookupItemBase()

    override x.Image = PsiSymbolsThemedIcons.Keyword.Id

    override x.Text =
        match keywordSuffix with
        | KeywordSuffix.Space -> keyword + " "
        | KeywordSuffix.Quotes -> keyword + " \"\""
        | _ -> keyword

    override x.GetDisplayName() = LookupUtil.FormatLookupString(keyword, x.TextColor)

    override x.Accept(textControl, nameRange, insertType, suffix, solution, keepCaret) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaret)

        match keywordSuffix with
        | KeywordSuffix.Quotes ->
            // Move caret back inside inserted quotes.
            textControl.Caret.MoveTo(textControl.Caret.Offset() - 1, CaretVisualPlacement.DontScrollIfVisible)
            textControl.RescheduleCompletion(solution)
        | _ -> ()

    interface IRiderAsyncCompletionLookupItem


type FSharpKeywordLookupItem(keyword, description, suffix) =
    inherit FSharpKeywordLookupItemBase(keyword, suffix)

    interface IDescriptionProvidingLookupItem with
        member x.GetDescription() = RichTextBlock(description)


type FSharpHashDirectiveLookupItem(directive, suffix) =
    inherit FSharpKeywordLookupItemBase(directive, suffix)


[<SolutionComponent>]
type FSharpHashDirectiveAutocompletionStrategy() =
    interface IAutomaticCodeCompletionStrategy with
        member x.Language = FSharpLanguage.Instance :> _

        member x.AcceptsFile(file, textControl) =
            match file.GetSourceFile() with
            | null -> false
            | sourceFile -> sourceFile.LanguageType.Is<FSharpScriptProjectFileType>()

        member x.AcceptTyping(char, _, _) = char = '#'
        member x.ProcessSubsequentTyping(char, _) = char.IsLetterFast()

        member x.IsEnabledInSettings(_, _) = AutopopupType.SoftAutopopup
        member x.ForceHideCompletion = false
