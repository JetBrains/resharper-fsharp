namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Comment

open JetBrains.ReSharper.Feature.Services.CodeCompletion.CompletionInDocComments
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi

[<Language(typeof<FSharpLanguage>)>]
type FSharpDocCommentElementsItemsProvider() =
    inherit DocCommentElementsItemsProviderBase()
