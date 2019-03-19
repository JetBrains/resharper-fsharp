namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open System
open JetBrains.Application
open JetBrains.Platform.MsBuildHost.Models
open JetBrains.ProjectModel
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Extensions
open JetBrains.ReSharper.Host.Features.ProjectModel.View.EditProperties.Projects.MsBuild
open JetBrains.ReSharper.Host.Features.ProjectModel.View.EditProperties.Projects.MsBuild.Providers
open JetBrains.ReSharper.Host.Features.ProjectModel.View.EditProperties.Utils
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.Util

[<ShellFeaturePart>]
type FSharpProjectPropertiesBuilder(projectPropertiesRequests) =
    inherit ManagedProjectPropertiesBuilder(projectPropertiesRequests)

    override x.IsApplicable(projectProperties) =
        projectProperties :? FSharpProjectProperties && base.IsApplicable(projectProperties)

    override x.BuildProjectBuildSettings(rdProjectDescriptor, rdProjects, buildSettings) =
        let fsBuildSettings = buildSettings :?> FSharpBuildSettings
        fsBuildSettings.TailCalls <-
            let mutable value = Unchecked.defaultof<bool>
            match bool.TryParse(rdProjectDescriptor.GetPropertyValue("Tailcalls"), &value) with
            | true -> value
            | _ -> false

        base.BuildProjectBuildSettings(rdProjectDescriptor, rdProjects, buildSettings)


[<SolutionComponent>]
type FSharpLanguageSpecificItemsProvider() =
    interface IMsBuildConfigurationTabProvider with
        member x.Order = Int32.MaxValue

        member x.CreateSections(lifetime, project, properties) =
            if project.ProjectProperties :? FSharpProjectProperties then
                [| EditPropertyItemBuilder
                       .Section(ConfigurationTabProvider.CompileSectionTitle)
                       .AddCheckBox(properties, "Tailcalls", "Generate tail calls") |] :> _

            else EmptyList.Instance :> _
