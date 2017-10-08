namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Services.Cs.CodeCompletion
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open Microsoft.FSharp.Compiler.SourceCodeServices

[<Language(typeof<FSharpLanguage>)>]
type FSharpKeywordsProvider() =
    inherit FSharpItemsProviderBase()

    let keywords = Keywords.KeywordsWithDescription |> List.map (fun (kw, descr) -> FSharpKeywordLookupItem(kw, descr))

    override x.IsAvailable(_) = true

    override x.AddLookupItems(context, collector) =
        if not context.ShouldComplete then false else

        match context.FsCompletionContext with
        | Some (CompletionContext.Invalid) -> false
        | _ ->

        match context.TokenAtCaret, context.TokenBeforeCaret with
        | null, _ | _, null -> false
        | tokenAtCaret, tokenBefore ->

        let tokenType = tokenAtCaret.GetTokenType()
        let tokenBeforeType = tokenBefore.GetTokenType()

        if tokenBeforeType = FSharpTokenType.LINE_COMMENT ||
           tokenBeforeType = FSharpTokenType.DEAD_CODE ||
           tokenBeforeType = FSharpTokenType.DOT ||
           tokenType = FSharpTokenType.DEAD_CODE ||
           tokenBefore = context.TokenAtCaret && isNotNull tokenBeforeType &&
               (tokenBeforeType.IsComment || tokenBeforeType.IsStringLiteral || tokenBeforeType.IsConstantLiteral)
        then false else

        if (not (List.isEmpty (fst context.Names))) then false else

        for keyword in keywords do
            keyword.InitializeRanges(x.GetDefaultRanges(context), context.BasicContext)
            collector.Add(keyword)

        true
