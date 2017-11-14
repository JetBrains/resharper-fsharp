namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open Microsoft.FSharp.Compiler.SourceCodeServices

[<Language(typeof<FSharpLanguage>)>]
type FSharpKeywordsProvider() =
    inherit FSharpItemsProviderBase()

    let keywords =
        Keywords.KeywordsWithDescription
        // todo: implement auto-popup completion strategy that will cover operators
        |> List.filter (fun (keyword, _) -> not (PrettyNaming.IsOperatorName keyword))
        |> List.map (fun (keyword, description) -> FSharpKeywordLookupItem(keyword, description))

    override x.IsAvailable(_) = true

    override x.AddLookupItems(context, collector) =
        match context.FsCompletionContext with
        | Some (CompletionContext.Invalid) -> false
        | _ ->

        match context.TokenBeforeCaret with
        | null -> false
        | tokenBefore ->

        let tokenBeforeType = tokenBefore.GetTokenType()

        if tokenBeforeType = FSharpTokenType.LINE_COMMENT ||
           tokenBeforeType = FSharpTokenType.DEAD_CODE ||
           tokenBeforeType = FSharpTokenType.DOT ||
           tokenBefore = context.TokenAtCaret && isNotNull tokenBeforeType &&
               (tokenBeforeType.IsComment || tokenBeforeType.IsStringLiteral || tokenBeforeType.IsConstantLiteral)
        then false else

        if not context.PartialLongName.QualifyingIdents.IsEmpty then false else

        for keyword in keywords do
            keyword.InitializeRanges(context.Ranges, context.BasicContext)
            collector.Add(keyword)

        true
