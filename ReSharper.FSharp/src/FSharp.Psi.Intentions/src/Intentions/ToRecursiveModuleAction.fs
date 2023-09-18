namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.ContextActions
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl

[<ContextAction(Name = "ToRecursiveModule", Group = "F#", Description = "To recursive")>]
[<ZoneMarker(typeof<ILanguageFSharpZone>, typeof<IProjectModelZone>, typeof<ITextControlsZone>, typeof<PsiFeaturesImplZone>)>]
type ToRecursiveModuleAction(dataProvider: FSharpContextActionDataProvider) =
    inherit ContextActionBase()

    override x.Text = "To recursive"

    override x.IsAvailable _ =
        let moduleDeclaration = dataProvider.GetSelectedElement<IDeclaredModuleLikeDeclaration>()
        if isNull moduleDeclaration || moduleDeclaration.IsRecursive then false else

        isAtModuleDeclarationKeyword dataProvider moduleDeclaration

    override x.ExecutePsiTransaction(_, _) =
        use cookie = FSharpExperimentalFeatureCookie.Create(ExperimentalFeature.Formatter)
        let moduleLikeDeclaration = dataProvider.GetSelectedElement<IDeclaredModuleLikeDeclaration>()
        moduleLikeDeclaration.SetIsRecursive(true)

        null
