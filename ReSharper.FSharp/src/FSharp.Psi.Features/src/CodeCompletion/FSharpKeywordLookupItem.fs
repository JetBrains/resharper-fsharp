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

    interface IParameterInfoCandidatesProvider with
        member x.HasCandidates = true
        member x.CreateCandidates() = [x :> ICandidate] :> _

    interface ICandidate with
        member x.GetSignature(_, _, _, _, _) = RichText(keyword)
        member x.GetDescription() = RichTextBlock(description)
        member x.Matches(_) = true

        member x.GetParametersInfo(_, _) = ()
        member val IsFilteredOut = false with get, set
        member x.PositionalParameterCount = 0
        member x.IsObsolete = false
        member x.ObsoleteDescription = null
