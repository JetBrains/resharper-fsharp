namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.FileTemplates

open System
open JetBrains.Application
open JetBrains.Application.UI.Options
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope
open JetBrains.ReSharper.Host.Features.LiveTemplates.Scope
open JetBrains.ReSharper.LiveTemplates.UI
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties

type InAzureFunctionsFSharpProject() =
    inherit InAzureFunctionsProject()
    static let ourDefaultGuid = Guid("6EAE234E-60AA-410E-B021-D219A2478F98")

    override x.GetDefaultUID() = ourDefaultGuid
    override x.PresentableShortName = "Azure Functions (F#) projects"

[<ShellComponent>]
type AzureFSharpScopeProvider() as this =
    inherit AzureProjectScopeProvider()

    do this.Creators.Add(fun x -> this.TryToCreateScope(x) :> _)

    member this.TryToCreateScope(x) = base.TryToCreate<InAzureFunctionsFSharpProject>(x)

    override x.GetLanguageSpecificScopePoints project =
        let baseItems = base.GetLanguageSpecificScopePoints project
        seq {
            yield! baseItems
            if project.ProjectProperties :? FSharpProjectProperties then
                yield InAzureFunctionsFSharpProject()
        }

[<ScopeCategoryUIProvider(Priority = -41., ScopeFilter = ScopeFilter.Project)>]
type AzureFSharpProjectScopeCategoryUIProvider() as this =
    inherit ScopeCategoryUIProvider(JetBrains.ProjectModel.Resources.ProjectModelThemedIcons.Fsharp.Id)
    do
        this.MainPoint <- InAzureFunctionsFSharpProject()

    override x.BuildAllPoints() = seq { yield InAzureFunctionsFSharpProject() }
    override x.CategoryCaption = "Azure (F#)"

[<OptionsPage("RiderAzureFSharpFileTemplatesSettings", "F#", typeof<ProjectModelThemedIcons.Fsharp>)>]
type RiderAzureFSharpFileTemplatesOptionPage
        (lifetime, optionsPageContext, settings, storedTemplatesProvider, uiProvider: AzureFSharpProjectScopeCategoryUIProvider,
         scopeCategoryManager, uiFactory, iconHost, dialogHost) =
    inherit RiderFileTemplatesOptionPageBase(lifetime, uiProvider, optionsPageContext, settings, storedTemplatesProvider,
        scopeCategoryManager, uiFactory, iconHost, dialogHost, FSharpProjectFileType.Name)
