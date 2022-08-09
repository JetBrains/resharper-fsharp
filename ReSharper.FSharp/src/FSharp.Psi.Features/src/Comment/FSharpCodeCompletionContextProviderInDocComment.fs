namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Comment

open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.CompletionInDocComments
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

[<IntellisensePart>]
type FSharpCodeCompletionContextProviderInDocComment() =
    inherit CodeCompletionContextProviderInDocCommentBase()

    override x.IsApplicable(context) = context.File :? IFSharpFile

    override x.GetTokenNode(context) =

        let file = context.File.As<IFSharpFile>();
        if isNull file then null else

        //if context.CodeCompletionType = CodeCompletionType.BasicCompletion then null else

        let selectedTreeRange = context.SelectedTreeRange
        if not (selectedTreeRange.IsValid()) || selectedTreeRange.Length > 0 then null else

        let token = context.File.FindTokenAt(selectedTreeRange.StartOffset - 1).As<ICommentNode>()
        if token = null then null else

        let blockComment = DocCommentBlockNodeNavigator.GetByDocCommentNode(token);
        if blockComment = null then null else

        token
