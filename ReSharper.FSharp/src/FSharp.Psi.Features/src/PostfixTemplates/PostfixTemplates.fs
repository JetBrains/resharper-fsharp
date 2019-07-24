namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open JetBrains.ReSharper.Feature.Services.CodeCompletion.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Services.Cs.CodeCompletion
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<AllowNullLiteral>]
type FSharpPostfixTemplateContext(treeNode: ITreeNode, executionContext: PostfixTemplateExecutionContext) as this =
    inherit PostfixTemplateContext(treeNode, executionContext)

    override x.Language = FSharpLanguage.Instance :> _

    override x.GetAllExpressionContexts() =
        [| FSharpPostfixExpressionContext(this, treeNode) :> PostfixExpressionContext |] :> _


and FSharpPostfixExpressionContext(postfixContext, expression) =
    inherit PostfixExpressionContext(postfixContext, expression)


//[<Language(typeof<FSharpLanguage>)>]
type FSharpPostfixTemplateContextFactory() =
    interface IPostfixTemplateContextFactory with
        member x.GetReparseStrings() = EmptyArray.Instance

        member x.TryCreate(treeNode, executionContext) =
            if isNull treeNode then null else
            FSharpPostfixTemplateContext(treeNode, executionContext) :> _


//[<Language(typeof<FSharpLanguage>)>]
type FSharpPostfixTemplatesProvider(templatesManager, sessionExecutor, usageStatistics) =
    inherit PostfixTemplatesItemProviderBase<FSharpCodeCompletionContext, FSharpPostfixTemplateContext>(
        templatesManager, sessionExecutor, usageStatistics)

    override x.TryCreatePostfixContext(fsCompletionContext) =
        let context = fsCompletionContext.BasicContext
        let settings = context.ContextBoundSettingsStore
        let executionContext = PostfixTemplateExecutionContext(context.Solution, context.TextControl, settings, "__")

        match fsCompletionContext.TokenBeforeCaret with
        | null -> null
        | treeNode -> FSharpPostfixTemplateContext(treeNode, executionContext)
