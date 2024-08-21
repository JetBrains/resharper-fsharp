namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Comment

open JetBrains.Application.Parts
open JetBrains.ReSharper.Feature.Services.CodeCompletion.CompletionInDocComments
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

[<IntellisensePart(Instantiation.DemandAnyThreadSafe)>]
type FSharpCodeCompletionContextProviderInDocComment() =
    inherit CodeCompletionContextProviderInDocCommentBase()

    override x.IsApplicable(context) = context.File :? IFSharpFile

    override x.GetTokenNode(context) =
        match context.File with
        | null -> null
        | file ->

        let selectedTreeRange = context.SelectedTreeRange
        if not (selectedTreeRange.IsValid()) || selectedTreeRange.Length > 0 then null else

        match file.FindTokenAt(selectedTreeRange.StartOffset - 1) with
        | :? ICommentNode as token ->
            let blockComment = DocCommentBlockNodeNavigator.GetByDocCommentNode(token);
            if isNull blockComment then null else token
        | _ -> null
