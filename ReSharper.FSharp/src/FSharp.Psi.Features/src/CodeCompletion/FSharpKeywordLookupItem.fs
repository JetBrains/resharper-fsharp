namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.ParameterInfo
open JetBrains.ReSharper.Psi.Resources
open JetBrains.UI.RichText

type FSharpKeywordLookupItem(keyword, description: string) =
    inherit TextLookupItemBase()

    override x.Image = PsiSymbolsThemedIcons.Keyword.Id
    override x.Text = keyword

    interface IDescriptionProvidingLookupItem with
        member x.GetDescription() = RichTextBlock(description)
