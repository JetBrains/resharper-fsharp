namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.ReSharper.Host.Features.QuickDefinition
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpQuickDefinitionService() =
    inherit DefaultQuickDefinitionLanguageService()

    override x.GetPresentableTreeRange(node) =
        match node with
        | :? IFSharpPattern as fsPattern ->
            let fsPattern = skipIntermediatePatParents fsPattern
            let binding = BindingNavigator.GetByHeadPattern(fsPattern)
            TreeRange(LetBindingsNavigator.GetByBinding(binding)) :> _

        | _ -> base.GetPresentableTreeRange(node)
