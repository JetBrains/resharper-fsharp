namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host

open System
open JetBrains.Application
open JetBrains.Platform.MsBuildHost.Models
open JetBrains.ProjectModel
open JetBrains.ProjectModel.MSBuild
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Extensions
open JetBrains.ReSharper.Host.Core.Features.ProjectModel.View.EditProperties.Projects.MsBuild
open JetBrains.ReSharper.Host.Core.Features.ProjectModel.View.EditProperties.Projects.MsBuild.Providers
open JetBrains.ReSharper.Host.Core.Features.ProjectModel.View.EditProperties.Utils
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
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

    override this.BuildProjectConfiguration(rdProjectDescriptor, project, configuration) =
        base.BuildProjectConfiguration(rdProjectDescriptor, project, configuration)

        let languageVersion = project.GetPropertyValueIgnoreCase(MSBuildProjectUtil.LanguageVersionProperty)
        let languageVersion = FSharpLanguageVersion.parseCompilationOption languageVersion 

        let configuration = configuration.As<IFSharpProjectConfiguration>()
        configuration.LanguageVersion <- languageVersion 


[<SolutionComponent>]
type FSharpLanguageSpecificItemsProvider() =
    interface IMsBuildConfigurationTabProvider with
        member x.Order = Int32.MaxValue

        member x.CreateSections(_, project, properties, _) =
            if project.ProjectProperties :? FSharpProjectProperties then
                [| EditPropertyItemBuilder
                       .Section(ConfigurationTabProvider.CompileSectionTitle)
                       .AddCheckBox(properties, "Tailcalls", "Generate tail calls") |] :> _

            else EmptyList.Instance :> _

        member x.GetTabTitle _ = "General"
