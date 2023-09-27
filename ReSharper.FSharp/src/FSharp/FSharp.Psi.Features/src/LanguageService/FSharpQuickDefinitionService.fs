namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Util
open JetBrains.Rider.Backend.Features.QuickDefinition
open JetBrains.Application.BuildScript.Application.Zones

[<Language(typeof<FSharpLanguage>)>]
[<ZoneMarker(typeof<JetBrains.Rider.Backend.Product.IRiderProductEnvironmentZone>, typeof<JetBrains.Rider.Backend.Env.IRiderFeatureZone>)>]
type FSharpQuickDefinitionService() =
    inherit DefaultQuickDefinitionLanguageService()

    override x.GetPresentableTreeRange(node) =
        match node with
        | :? IFSharpPattern as fsPattern ->
            let fsPattern = skipIntermediatePatParents fsPattern
            let binding = BindingNavigator.GetByHeadPattern(fsPattern)
            TreeRange(LetBindingsNavigator.GetByBinding(binding)) :> _

        | _ -> base.GetPresentableTreeRange(node)
